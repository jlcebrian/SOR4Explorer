using System;
using System.Drawing;
using System.Drawing.Drawing2D;

static class GraphicExtensions
{
    private static GraphicsPath GetCapsule(RectangleF baseRect)
    {
        float diameter;
        RectangleF arc;
        GraphicsPath path = new GraphicsPath();
        try
        {
            if (baseRect.Width > baseRect.Height)
            {
                diameter = baseRect.Height;
                SizeF sizeF = new SizeF(diameter, diameter);
                arc = new RectangleF(baseRect.Location, sizeF);
                path.AddArc(arc, 90, 180);
                arc.X = baseRect.Right - diameter;
                path.AddArc(arc, 270, 180);
            }
            else if (baseRect.Width < baseRect.Height)
            {
                diameter = baseRect.Width;
                SizeF sizeF = new SizeF(diameter, diameter);
                arc = new RectangleF(baseRect.Location, sizeF);
                path.AddArc(arc, 180, 180);
                arc.Y = baseRect.Bottom - diameter;
                path.AddArc(arc, 0, 180);
            }
            else
            {
                path.AddEllipse(baseRect);
            }
        }
        catch (Exception)
        {
            path.AddEllipse(baseRect);
        }
        finally
        {
            path.CloseFigure();
        }
        return path;
    }

    private static GraphicsPath GetRoundedRect(RectangleF baseRect, float radius)
    {
        if (radius <= 0.0F)
        {
            GraphicsPath mPath = new GraphicsPath();
            mPath.AddRectangle(baseRect);
            mPath.CloseFigure();
            return mPath;
        }
        if (radius >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
            return GetCapsule(baseRect);

        float diameter = radius * 2.0F;
        SizeF sizeF = new SizeF(diameter, diameter);
        RectangleF arc = new RectangleF(baseRect.Location, sizeF);
        GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

        path.AddArc(arc, 180, 90);
        arc.X = baseRect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = baseRect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = baseRect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, 
        Rectangle rectangle, float radius)
    {
        GraphicsPath path = GetRoundedRect(rectangle, radius);
        graphics.DrawPath(pen, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen,
        float x, float y, float width, float height, float radius)
    {
        RectangleF rectangle = new RectangleF(x, y, width, height);
        GraphicsPath path = GetRoundedRect(rectangle, radius);
        graphics.DrawPath(pen, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, 
        int x, int y, int width, int height, int radius)
    {
        float fx = Convert.ToSingle(x);
        float fy = Convert.ToSingle(y);
        float fwidth = Convert.ToSingle(width);
        float fheight = Convert.ToSingle(height);
        float fradius = Convert.ToSingle(radius);
        graphics.DrawRoundedRectangle(pen, fx, fy, fwidth, fheight, fradius);
    }
}
