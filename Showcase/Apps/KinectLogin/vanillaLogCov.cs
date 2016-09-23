using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;
using Matrix = MathNet.Numerics.LinearAlgebra.Generic.Matrix<float>;
using Evd = MathNet.Numerics.LinearAlgebra.Single.Factorization.Evd;


namespace KinectLogin
{
    //modified from kam's original code into more portable class
    //not full check over logic, glanced. <= TODO

    class vanillaLogCov
    {

        //put variables here
        DenseMatrix cov1, cov2;
        Matrix diag, logcov1, logcov2;
        Evd evd; //factorization storage
        float distance;

        public vanillaLogCov(float[,] matrix1, float[,] matrix2)
        {
            //deep copy the covariances
            //densematrix allocates new memory for the matrix
            cov1 = new DenseMatrix(Covariance(matrix1));
            cov2 = new DenseMatrix(Covariance(matrix2));
            evd = cov1.Evd();
            diag = evd.D();
            //could make this a function but thats for later cleanup.... < . <
            //should be square matrix so matrix lengths dont matter
            for (int i = 0; i < diag.RowCount; i++)
            {
                diag[i, i] = (float)Math.Log(Math.Abs((double)diag[i, i]));
            }
            logcov1 = evd.EigenVectors() * diag * evd.EigenVectors().Transpose();

            evd = cov2.Evd();
            diag = evd.D();
            //could make this a function but thats for later cleanup.... < . <
            //should be square matrix so matrix lengths dont matter
            for (int i = 0; i < diag.RowCount; i++)
            {
                diag[i, i] = (float)Math.Log(Math.Abs((double)diag[i, i]));
            }
            logcov2 = evd.EigenVectors() * diag * evd.EigenVectors().Transpose();

            //compute distance
            distance = 0; //initialize to empty
            for (int i = 0; i < logcov2.RowCount; i++)
            {
                for (int j = 0; j < logcov2.RowCount; j++)
                {
                    distance += (float)Math.Pow(logcov1[i, j] - logcov2[i, j], 2);
                }
            }
            distance = (float)Math.Sqrt(distance);
        }

        public float getDistance()
        {
            return distance;
        }
        private unsafe static float[,] SubtractMeans(float[,] matrix)
        {
            var x = matrix.GetLength(0);
            var y = matrix.GetLength(1);
            var result = new float[x, y];
            fixed (float* mp = matrix)
            fixed (float* rp = result)
            {
                for (int i = 0; i < y; i++)
                {
                    var tmp = 0d;
                    for (int j = 0; j < x; j++)
                        tmp += mp[j * y + i];
                    var mean = tmp / x;
                    for (int j = 0; j < x; j++)
                        rp[j * y + i] = mp[j * y + i] - (float)mean;
                }
            }

            return result;
        }
        public static float[,] Transpose(float[,] matrix)
        {
            var x = matrix.GetLength(0);
            var y = matrix.GetLength(1);
            var result = new float[y, x];
            for (int i = 0; i < x; i++)
                for (int j = 0; j < y; j++)
                    result[j, i] = matrix[i, j];
            return result;
        }
        public unsafe static float[,] Multiply(float[,] matrix1, float[,] matrix2)
        {
            var x = matrix1.GetLength(0);
            var y = matrix2.GetLength(1);
            var z = matrix1.GetLength(1);
            if (matrix1.GetLength(1) != matrix2.GetLength(0))
                throw new InvalidOperationException("Can't multiply");
            var result = new float[x, y];
            fixed (float* p1 = matrix1)
            fixed (float* p2 = matrix2)
            fixed (float* p3 = result)
            {
                for (int i = 0; i < x; i++)
                    for (int j = 0; j < y; j++)
                    {
                        float tmp = p3[i * y + j];
                        for (int k = 0; k < x; k++)
                            tmp += p1[i * z + k] * p2[k * y + j];
                        p3[i * y + j] = tmp;
                    }
            }
            return result;
        }
        public unsafe static float[,] Covariance(float[,] matrix)
        {
            var n = SubtractMeans(matrix);
            var t = Transpose(n);
            var m = Multiply(t, n);
            var x = m.GetLength(0);
            var y = m.GetLength(1);
            fixed (float* mp = m)
            {
                for (int i = 0; i < x; i++)
                    for (int j = 0; j < y; j++)
                    {
                        var tmp = mp[i * x + j];
                        tmp /= y - 1;
                        mp[i * x + j] = tmp;
                    }
            }
            return m;
        }
    }
}
