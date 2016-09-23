using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectLogin
{
    class RecordingBuffer
    {
        int bufferLength, currentFrame, numJoints, steadyCounter; //frames in buffer
        int statesToSteady = 17;
        float[,] bufferData; //can make more efficient eventually...
        float[] prevJoints; //joints of start state to detect movement
        float[][,] allBufferData;
        public bool bufferReady;
        MovingState ms; //"hacky" version of fuzzy logic
        enum MovingState {IDLE, STOPPED, MOVING };
        int distanceAll;
        public RecordingBuffer(int bufferLength, int numJoints)
        {
            this.numJoints = numJoints;
            steadyCounter = 0;
            this.bufferReady = false; 
            this.bufferLength = bufferLength;
            this.currentFrame = 0;
            this.allBufferData = new float[5][,];
            this.bufferData = new float[bufferLength,this.numJoints*3]; //constant integer length
            this.ms = MovingState.IDLE; // initial state
            distanceAll = 0;
        }

        public bool isMoving()
        {
            if (this.ms == MovingState.MOVING)
            {
                return true;
            }
            return false;
        }

        public bool getBufferStatus()
        {
            return bufferReady;
        }

        public float[,] getBufferData()
        {
            return bufferData;
        }

        public int getBufferNumberFrames()
        {
            return currentFrame; //exclude 0 count
        }

        public void addFrame(float[] dataVector, int recordNumber)
        {
            float distanceAll = 0, distanceThreshold = .2f; //distances of the sum of joint-to-joint distances from the prev. stored point

            //data vector must be of size -- TODO spit out some error code at some point in time
            if (dataVector.Length != this.numJoints * 3) 
                return;
            
            //movement-detection logic
            switch (this.ms)
            {
                case MovingState.IDLE:
                    //initialize
                    prevJoints = new float[numJoints * 3];
                    Array.Copy(dataVector, prevJoints, numJoints * 3);
                    this.ms = MovingState.STOPPED;
                    return; 
                case MovingState.STOPPED:
                    for (int i = 0; i < numJoints; i++)
                    {
                        distanceAll += (float)Math.Sqrt(Math.Pow(dataVector[i * 3] - prevJoints[i * 3], 2) +
                                      Math.Pow(dataVector[i * 3 + 1] - prevJoints[i * 3 + 1], 2) +
                                      Math.Pow(dataVector[i * 3 + 2] - prevJoints[i * 3 + 2], 2));
                    }
                    if (distanceAll > distanceThreshold)
                    {
                        this.ms = MovingState.MOVING;
                    }
                    else
                    {
                        Array.Copy(dataVector, prevJoints, numJoints * 3); //update last joints
                    }
                    break;
                case MovingState.MOVING:

                    //add to buffer
                    for (int i = 0; i < this.numJoints * 3; i++)
                    {
                        //lazy copy
                        this.bufferData[this.currentFrame, i] = dataVector[i];
                    }                    
                    this.currentFrame++; //increment at end
                    if (this.currentFrame >= bufferLength)
                    {
                        this.bufferReady = true; //hit MAX capacity
                        this.allBufferData[recordNumber] = this.bufferData;
                        return;
                    }                    
                    //check if ACTOR has stopped
                    for (int i = 0; i < numJoints; i++)
                    {
                        distanceAll += (float)Math.Sqrt(Math.Pow(dataVector[i * 3] - prevJoints[i * 3], 2) +
                                      Math.Pow(dataVector[i * 3 + 1] - prevJoints[i * 3 + 1], 2) +
                                      Math.Pow(dataVector[i * 3 + 2] - prevJoints[i * 3 + 2], 2));
                    }
                    //set prev joints
                    prevJoints = new float[numJoints * 3];
                    Array.Copy(dataVector, prevJoints, numJoints * 3);

                    if (distanceAll < distanceThreshold)
                    {
                        steadyCounter++;
                        if (steadyCounter > statesToSteady)
                        {
                            this.bufferReady = true;
                            this.allBufferData[recordNumber] = this.bufferData;

                            return;
                        }
                    }
                    else
                    {
                        steadyCounter = 0; //reset counter
                    }

                    break;
                default:
                    break;
            }
        }

        public void clearBuffer()
        {
            steadyCounter = 0;
            this.ms = MovingState.IDLE;
            this.bufferReady = false;
            this.currentFrame = 0;
            this.distanceAll = 0;
        }
    }


}
