using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils.ShapeTools
{
    public static class RectangleTools
    {
        public static Rectangle RoundRectF( RectangleF rectf )
        {
            return new Rectangle( (int)( rectf.X + 0.5 ),
                                  (int)( rectf.Y + 0.5 ),
                                  (int)( rectf.Width + 0.5 ),
                                  (int)( rectf.Height + 0.5 ) );
        }
    }
}
