using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcaseCommon.Kinect_Processing
{
    public static class KinectManagerConfigReader
    {
        private const string CONFIG_FILE = "manager_config.txt";
        private const string CONFIG_SKIP = "#";

        private const string JITTER = "jitter";
        private const string UNCERTAINTY = "uncertainty";
        private const string XFILTER = "hand.xFilter";
        private const string YFITLER = "hand.yFilter";
        private const string XFILTERPARAMS = "hand.xFilterParams";
        private const string YFILTERPARAMS = "hand.yFilterParams";
        private const string HANDCENTER = "hand.rectCenter";
        private const string HANDSIZE = "hand.rectSize";


        public static KinectManager.Config GetConfig()
        {
            KinectManager.Config result = new KinectManager.Config();
            try
            {
                string configPath = "C:\\KinectShowcase\\manager_config.txt";
                if (File.Exists(configPath))
                {
                    using (StreamReader configStream = new StreamReader(configPath))
                    {
                        int lineCount = 0;
                        while (!configStream.EndOfStream)
                        {
                            lineCount++;

                            string line = configStream.ReadLine();

                            //check if this line is a comment
                            if (line.Length >= 1 && !line.Substring(0, 1).Equals(CONFIG_SKIP))
                            {
                                string[] parts = line.Split('=').Select(sValue => sValue.Trim()).ToArray();
                                if (parts.Length == 2)
                                {
                                    string configVal = parts[0].ToLowerInvariant();
                                    string configData = parts[1].ToLowerInvariant();
                                    if (configVal == JITTER.ToLowerInvariant())
                                    {
                                        float value;
                                        if (float.TryParse(configData, out value))
                                        {
                                            result.KalmanJitterRadius = value;
                                        }
                                    }
                                    else if (configVal == UNCERTAINTY.ToLowerInvariant())
                                    {
                                        float value;
                                        if (float.TryParse(configData, out value))
                                        {
                                            result.KalmanMeasurementUncertainty = value;
                                        }
                                    }
                                    else if (configVal == XFILTER.ToLowerInvariant())
                                    {
                                        KinectHandManager.FilterType filterType = KinectHandManager.StringToFilterType(configData);
                                        result.HandConfig.XFilter = filterType;
                                    }
                                    else if (configVal == YFITLER.ToLowerInvariant())
                                    {
                                        KinectHandManager.FilterType filterType = KinectHandManager.StringToFilterType(configData);
                                        result.HandConfig.YFilter = filterType;
                                    }
                                    else if (configVal == XFILTERPARAMS.ToLowerInvariant())
                                    {
                                        result.HandConfig.XFilterParams = ParseCommaSeparatedFloats(configData);
                                    }
                                    else if (configVal == YFILTERPARAMS.ToLowerInvariant())
                                    {
                                        result.HandConfig.YFilterParams = ParseCommaSeparatedFloats(configData);
                                    }
                                    else if (configVal == HANDCENTER.ToLowerInvariant())
                                    {
                                        float[] values = ParseCommaSeparatedFloats(configData);
                                        if (values.Length == 2)
                                        {
                                            result.HandConfig.HandRectCenter = new Point(values[0], values[1]);
                                        }
                                    }
                                    else if (configVal == HANDSIZE.ToLowerInvariant())
                                    {
                                        float[] values = ParseCommaSeparatedFloats(configData);
                                        if (values.Length == 2)
                                        {
                                            result.HandConfig.HandRectSize = new Size(values[0], values[1]);
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("KinectManagerConfigReader - LOG - Unknown prefix: " + configVal);
                                    }
                                }
                                else
                                {
                                    //line wasn't long enough to be valid
                                    Debug.WriteLine("KinectManagerConfigReader - WARN - Line " + lineCount + "was too short be valid");
                                }
                            }
                        }

                        configStream.Close();
                    }
                }
            }
            catch (IOException e)
            {
                //error
                Debug.WriteLine("App - EXCEPTION - Exception Message: " + e.Message);
            }

            return result;
        }


        private static float[] ParseCommaSeparatedFloats(string aData)
        {
            string[] values = aData.Split(',').Select(sValue => sValue.Trim()).ToArray();
            List<float> floats = new List<float>();
            foreach (string cur in values)
            {
                float value;
                if (float.TryParse(cur, out value))
                {
                    floats.Add(value);
                }
            }
            return floats.ToArray();
        }
    }
}
