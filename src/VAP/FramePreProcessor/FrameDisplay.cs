// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using OpenCvSharp;

namespace FramePreProcessor
{
    public class FrameDisplay
    {
        private static readonly Dictionary<string, string> s_displayKVpairs = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> s_prev_displayKVpairs = new Dictionary<string, string>();

        public static void Display(Mat resizedFrame)
        {
            Mat frameToDisplay = resizedFrame.Clone();
            if (s_displayKVpairs.Count > 0)
            {
                double scale = 0.75;

                //Header box configs
                int boxHeight = (int)(80 * scale);
                int boxWidth = frameToDisplay.Width;
                int boxpadding = (int)(15 * scale);

                //Text configs
                int leftTextPadding = (int)(50 * scale);
                int textPadding = (int)(10 * scale);
                int textHeight = (int)(22 * scale);

                string row1Text = "Total:";
                string row2Text = "Network:";

                int row1Textbox = (int)(20 * row1Text.Length * scale);
                int row2Textbox = (int)(20 * row2Text.Length * scale);

                Cv2.Rectangle(frameToDisplay, new Rect(0, boxpadding, boxWidth, boxHeight), new Scalar(255, 255, 240), Cv2.FILLED);

                //Draw row 1: total count
                int row1Height = textHeight + textPadding + boxpadding;

                string result = "";
                foreach (string dir in s_displayKVpairs.Keys)
                {
                    int displayTotal = int.Parse(s_displayKVpairs[dir]);
                    result += dir + " " + displayTotal + "       ";

                }
                Cv2.PutText(frameToDisplay, result, new Point(leftTextPadding + row1Textbox, row1Height), HersheyFonts.HersheyPlain, scale, Scalar.Black);
            }

            Cv2.ImShow("Raw Frame", frameToDisplay);
            //Cv2.WaitKey(1);
        }

        public static void UpdateKVPairs(Dictionary<string, string> kvpairs)
        {
            foreach (string s in kvpairs.Keys)
            {
                if (!s_displayKVpairs.ContainsKey(s))
                {
                    s_displayKVpairs.Add(s, kvpairs[s]);
                }
                else
                {
                    int currentVal = int.Parse(s_displayKVpairs[s]);
                    currentVal += int.Parse(kvpairs[s]);
                    s_displayKVpairs[s] = currentVal.ToString();
                }
            }
        }

    }
}
