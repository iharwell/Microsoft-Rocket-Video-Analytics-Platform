using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using LineDetector;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    public class LineCrossingProcessor : IProcessor
    {
        Detector detector;

        public LineCrossingProcessor( string lineFile, int samplingFactor, double resFactor, ISet<string > categories, Color bbColor, bool display = false )
        {
            Categories = categories;
            BoundingBoxColor = bbColor;

            detector = new Detector( samplingFactor, resFactor, lineFile, display );
            var lines = detector.multiLaneDetector.getAllLines();
            LineSegments = new Dictionary<string, LineSegment>();

            for ( int i = 0; i < lines.Count; i++ )
            {
                LineSegments.Add( lines[i].key, lines[i].segments );
            }
            DisplayOutput = display;
        }

        public Color BoundingBoxColor { get; set; }
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public ISet<string> Categories { get; set; }
        public bool DisplayOutput
        {
            get => detector.DISPLAY_BGS;
            set => detector.DISPLAY_BGS = value;
        }

        public bool Run( IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage )
        {
            if ( items is null || items.Count == 0 )
            {
                detector.updateLineResults( frame, items, this );
                if ( items.Count > 0 )
                {
                    return true;
                }
                return false;
            }

            detector.updateLineResults( frame, items, this );

            for ( int i = items.Count - 1; i >= 0; --i )
            {
                for ( int j = items[i].ItemIDs.Count - 1; j > 0; --j )
                {
                    if ( items[i].ItemIDs[j].SourceObject == this )
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
