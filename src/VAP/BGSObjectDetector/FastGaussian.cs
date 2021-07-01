// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace BGSObjectDetector
{
    public class FastGaussian
    {
        private double _sigma;

        private List<int> _medianSizes;
        private List<int> _iterations;
        /*private int _medianSize1;
        private int _medianSize2;
        private int _iterations1;
        private int _iterations2;*/

        private const int MaxSize = 5;

        public int InterStageThreshold { get; set; }

        public FastGaussian(double sigma, MatType type)
        {
            _sigma = sigma;

            _medianSizes = new List<int>();
            _iterations = new List<int>();
            InterStageThreshold = 0;

            BuildIterations(sigma);
            /*double sigSq = _sigma * _sigma;
            double medBlur1 = MedianBlurStdDev(_medianSize1);
            double medBlur2 = MedianBlurStdDev(_medianSize2);
            _iterations1 = (int)( sigSq / medBlur1);

            if( _iterations1 < 4 )
            {
                _iterations1 = 0;
            }
            sigSq -= _iterations1 * medBlur1;

            _iterations2 = (int)(sigSq / medBlur2);



            MatType depth = (type & 7);
            int SizeFactor;
            if((type&7)==MatType.CV_8U)
            {
                SizeFactor = 3;
            }
            else
            {
                SizeFactor = 4;
            }

            int size = (int)(sigma * SizeFactor * 2 + 1.5) | 1;

            sigma = Math.Max(sigma, 0);

            if (depth < MatType.CV_32F)
            {
                depth = MatType.CV_32F;
            }

            var kern = Cv2.GetGaussianKernel(size, sigma, depth);

            if ( kern == null )
            {
                throw new NullReferenceException();
            }

            terms = new List<float>(kern.Cols);
            for (int i = 0; i < kern.Rows; i++)
            {
                terms.Add(kern.Get<float>(i));
            }

            _kernel = new Mat(kern.Cols, kern.Cols, MatType.CV_32SC1);

            for (int i = 0; i < kern.Cols; i++)
            {
                for (int j = 0; j < kern.Cols; j++)
                {
                    _kernel.Set(i, j, kern.Get<float>(i) * kern.Get<float>(j));
                }
            }*/
            //_kernel = kern;
        }

        private void BuildIterations(double sigma)
        {
            int nextMedianSize = 3;
            double medSigma = MedianBlurStdDev(nextMedianSize);
            int medianIterations = 0;
            int prevIterations = 0;
            do
            {
                _medianSizes.Add(nextMedianSize);
                BuildIterations(sigma, _medianSizes);
                prevIterations = medianIterations;
                medianIterations = _iterations.Sum();
                if (medianIterations == prevIterations)
                {
                    break;
                }
                nextMedianSize += 2;
                if (nextMedianSize > MaxSize)
                {
                    break;
                }
            }
            while (medianIterations > 8)
                    ;

            if (medianIterations < 4)
            {
                _medianSizes.RemoveAt(_medianSizes.Count - 1);
                BuildIterations(sigma, _medianSizes);
            }
        }

        private void BuildIterations(double sigma, List<int> sizes)
        {
            if (!object.ReferenceEquals(sizes, _medianSizes))
            {
                _medianSizes.Clear();
                _iterations.Clear();
                for (int i = 0; i < sizes.Count; i++)
                {
                    _medianSizes.Add(sizes[i]);
                    _iterations.Add(0);
                }
            }
            else
            {
                _iterations.Clear();
                for (int i = 0; i < sizes.Count; i++)
                {
                    _iterations.Add(0);
                }
            }



            double runningSigmaSq = sigma * sigma;

            for (int i = sizes.Count - 1; i >= 0; --i)
            {
                double dev = MedianBlurStdDev(sizes[i]);
                int iterations = (int)(runningSigmaSq / dev);
                _iterations[i] = iterations;
                runningSigmaSq -= dev * iterations;
            }
        }

        public int Iterations(double targetSigma, int medianSize)
        {
            double sigSq = _sigma * _sigma;
            int it = (int)(0.5 + sigSq / MedianBlurStdDev(medianSize));
            return it;
        }

        public void Blur(ref UMat input, ref UMat output, MatType ddepth)
        {
            /*UMat k = new UMat(UMatUsageFlags.DeviceMemory);
            {
                _kernel.CopyTo(k);
                Cv2.SepFilter2D(input, output, ddepth, k, k);
                //Cv2.Filter2D(input, output, ddepth, k);
                k.Dispose();
            }*/
            bool swapAgain = false;
            for (int i = 0; i < _medianSizes.Count; i++)
            {
                int size = _medianSizes[i];
                for (int j = 0; j < _iterations[i]; j++)
                {
                    swapAgain = true;
                    Cv2.MedianBlur(input, output, size);
                    (input, output) = (output, input);
                    if (InterStageThreshold > 0)
                    {
                        Cv2.Threshold(input, output, InterStageThreshold, 255, ThresholdTypes.Binary);
                        (input, output) = (output, input);
                    }
                }
            }
            /*
            for (int i = 0; i < _iterations1; i++)
            {
                Cv2.MedianBlur(input, output, _medianSize1);
                (input, output) = (output, input);
            }
            for (int i = 0; i < _iterations2; i++)
            {
                Cv2.MedianBlur(input, output, _medianSize2);
                (input, output) = (output, input);
            }*/

            if (swapAgain)
            {
                (input, output) = (output, input);
            }
        }

        private static double MedianBlurStdDev(int blurSize)
        {
            double singleEvenDistDev = (blurSize * blurSize - 1) / 12.0;
            double dev2D = singleEvenDistDev * 2;

            return dev2D;
        }
    }
}
