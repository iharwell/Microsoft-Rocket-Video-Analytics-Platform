// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Utils.Config
{
    public static class OutputFolder
    {
        private static string s_outputFolderAll = "output_all/";
        private static string s_outputFolderXML = "output_xml/";
        private static string s_outputFolderVideo = "output_video/";
        private static string s_outputFolderBGSLine = "output_bgsline/";
        private static string s_outputFolderLtDNN = "output_ltdnn/";
        private static string s_outputFolderCcDNN = "output_ccdnn/";
        private static string s_outputFolderAML = "output_aml/";
        private static string s_outputFolderFrameDNNDarknet = "output_framednndarknet/";
        private static string s_outputFolderFrameDNNTF = "output_framednntf/";
        private static string s_outputFolderFrameDNNONNX = "output_framednnonnx/";
        private static string s_outputFolderBase = "../../";

        public static string OutputFolderBase { get => s_outputFolderBase; set => s_outputFolderBase = value; }
        public static string OutputFolderAll { get => s_outputFolderBase + s_outputFolderAll; }
        public static string OutputFolderXML { get => s_outputFolderBase + s_outputFolderXML; }
        public static string OutputFolderVideo { get => s_outputFolderBase + s_outputFolderVideo; }
        public static string OutputFolderBGSLine { get => s_outputFolderBase + s_outputFolderBGSLine; }
        public static string OutputFolderLtDNN { get => s_outputFolderBase + s_outputFolderLtDNN; }
        public static string OutputFolderCcDNN { get => s_outputFolderBase + s_outputFolderCcDNN; }
        public static string OutputFolderAML { get => s_outputFolderBase + s_outputFolderAML; }
        public static string OutputFolderFrameDNNDarknet { get => s_outputFolderBase + s_outputFolderFrameDNNDarknet; }
        public static string OutputFolderFrameDNNTF { get => s_outputFolderBase + s_outputFolderFrameDNNTF; }
        public static string OutputFolderFrameDNNONNX { get => s_outputFolderBase + s_outputFolderFrameDNNONNX; }
    }
}
