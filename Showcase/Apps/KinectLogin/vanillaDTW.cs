using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;

//modified vanilla DTW for multijoints
//http://data-matters.blogspot.com/2008/07/simple-implementation-of-dtwdynamic.html

namespace KinectLogin
{
    class SimpleDTW
    {
        //relative lengths of the query and gesture signals
        int qlength, glength;
        double[,] distance;
        double[,] f;
        ArrayList pathX;
        ArrayList pathY;
        ArrayList distanceList;
        double sum;

        public SimpleDTW(float[,] query, float[,] gesture, int _qlength, int _glength, int numjoints)
        {
            qlength = _qlength;
            glength = _glength;

            //modify to take in 2D array
            distance = new double[qlength, glength];

            for (int i = 0; i < qlength; i++)
            {
                for (int j = 0; j < glength; j++)
                {
                    distance[i, j] = 0;
                    //L2 distance between joints
                    for (int k = 0; k < numjoints; k++)
                    {
                        //modified distance to be instead of L2 between joints take the physical distances between joints


                        distance[i, j] += Math.Sqrt(Math.Pow(query[i, 3 * k] - gesture[j, 3 * k], 2)
                            + Math.Pow(query[i, 3 * k + 1] - gesture[j, 3 * k + 1], 2) +
                            Math.Pow(query[i, 3 * k + 2] - gesture[j, 3 * k + 2], 2));
                        //(1) normal L2
                        //(1) distance[i,j] += Math.Pow(query[i,k]-gesture[j,k],2);
                    }
                    //(1) normal L2
                    //(1)distance[i,j] = Math.Sqrt(distance[i,j]);
                }
            }

            f = new double[qlength + 1, glength + 1];

            for (int i = 0; i <= qlength; ++i)
            {
                for (int j = 0; j <= glength; ++j)
                {
                    f[i, j] = -1.0;
                }
            }

            for (int i = 1; i <= qlength; ++i)
            {
                f[i, 0] = double.PositiveInfinity;
            }
            for (int j = 1; j <= glength; ++j)
            {
                f[0, j] = double.PositiveInfinity;
            }

            f[0, 0] = 0.0;
            sum = 0.0;

            pathX = new ArrayList();
            pathY = new ArrayList();
            distanceList = new ArrayList();
        }

        public ArrayList getPathX()
        {
            return pathX;
        }

        public ArrayList getPathY()
        {
            return pathY;
        }

        public double getSum()
        {
            return sum;
        }

        public double[,] getFMatrix()
        {
            return f;
        }

        public ArrayList getDistanceList()
        {
            return distanceList;
        }

        public void computeDTW()
        {
            sum = computeFBackward(qlength, glength);
            //sum = computeFForward();
        }

        public double computeFForward()
        {
            for (int i = 1; i <= qlength; ++i)
            {
                for (int j = 1; j <= glength; ++j)
                {
                    if (f[i - 1, j] <= f[i - 1, j - 1] && f[i - 1, j] <= f[i, j - 1])
                    {
                        f[i, j] = distance[i - 1, j - 1] + f[i - 1, j];
                    }
                    else if (f[i, j - 1] <= f[i - 1, j - 1] && f[i, j - 1] <= f[i - 1, j])
                    {
                        f[i, j] = distance[i - 1, j - 1] + f[i, j - 1];
                    }
                    else if (f[i - 1, j - 1] <= f[i, j - 1] && f[i - 1, j - 1] <= f[i - 1, j])
                    {
                        f[i, j] = distance[i - 1, j - 1] + f[i - 1, j - 1];
                    }
                }
            }
            return f[qlength, glength];
        }

        public double computeFBackward(int i, int j)
        {
            if (!(f[i, j] < 0.0))
            {
                return f[i, j];
            }
            else
            {
                if (computeFBackward(i - 1, j) <= computeFBackward(i, j - 1) && computeFBackward(i - 1, j) <= computeFBackward(i - 1, j - 1)
                    && computeFBackward(i - 1, j) < double.PositiveInfinity)
                {
                    f[i, j] = distance[i - 1, j - 1] + computeFBackward(i - 1, j);
                }
                else if (computeFBackward(i, j - 1) <= computeFBackward(i - 1, j) && computeFBackward(i, j - 1) <= computeFBackward(i - 1, j - 1)
                    && computeFBackward(i, j - 1) < double.PositiveInfinity)
                {
                    f[i, j] = distance[i - 1, j - 1] + computeFBackward(i, j - 1);
                }
                else if (computeFBackward(i - 1, j - 1) <= computeFBackward(i - 1, j) && computeFBackward(i - 1, j - 1) <= computeFBackward(i, j - 1)
                    && computeFBackward(i - 1, j - 1) < double.PositiveInfinity)
                {
                    f[i, j] = distance[i - 1, j - 1] + computeFBackward(i - 1, j - 1);
                }
            }
            return f[i, j];
        }

    }
}
