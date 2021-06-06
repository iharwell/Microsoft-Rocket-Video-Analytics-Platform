using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Utils;
using Utils.Items;

namespace ProcessingPipeline
{
    public class HeavyDarknetProcessor : IProcessor
    {
        private DarknetDetector.CascadedDNNDarknet cascadedDNN;

        public HeavyDarknetProcessor( ISet<string> categories,
                                      double resFactor,
                                      Color boxColor,
                                      bool displayOutput )
        {
            cascadedDNN = new DarknetDetector.CascadedDNNDarknet( resFactor );
            Categories = categories;
            BoundingBoxColor = boxColor;
            DisplayOutput = displayOutput;
        }

        public HeavyDarknetProcessor( IList<(string, LineSegment)> lines,
                                      ISet<string> categories,
                                      double resFactor,
                                      Color boxColor,
                                      bool displayOutput )
            : this( categories, resFactor, boxColor, displayOutput )
        {
            if ( lines != null )
            {
                LineSegments = new Dictionary<string, LineSegment>( from entry in lines select new KeyValuePair<string, LineSegment>( entry.Item1, entry.Item2 ) );
            }
            else
            {
                LineSegments = new Dictionary<string, LineSegment>();
            }
        }
        public HeavyDarknetProcessor( IDictionary<string, LineSegment> lines,
                                      ISet<string> categories,
                                      double resFactor,
                                      Color boxColor,
                                      bool displayOutput )
            : this(categories, resFactor, boxColor, displayOutput)
        {
            LineSegments = lines;
        }

        public Color BoundingBoxColor { get; set; }
        public IDictionary<string, LineSegment> LineSegments { get; set; }
        public ISet<string> Categories { get; set; }
        public bool DisplayOutput { get; set; }

        public bool Run( IFrame frame, ref IList<IFramedItem> items, IProcessor previousStage )
        {
            items = cascadedDNN.Run( frame, items, LineSegments, Categories, this );

            if( items == null )
            {
                return false;
            }

            for ( int i = items.Count - 1; i >= 0; --i )
            {
                var item = items[i];
                for ( int j = item.ItemIDs.Count-1; j >= 0; --j )
                {
                    var id = item.ItemIDs[j];
                    if( id.SourceObject == this )
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
