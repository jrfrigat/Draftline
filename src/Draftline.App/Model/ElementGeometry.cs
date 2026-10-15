namespace Draftline.App.Model;

/// <summary>Rotation math shared by hit testing, selection and SVG links.</summary>
public static class ElementGeometry
{
    public static double Angle(double value) => double.IsFinite(value) ? ((value % 360) + 360) % 360 : 0;
    public static (double X, double Y) Rotate(double x, double y, Rect bounds, double angle)
    {
        var radians = angle * Math.PI / 180; var dx = x - bounds.CenterX; var dy = y - bounds.CenterY;
        return (bounds.CenterX + dx * Math.Cos(radians) - dy * Math.Sin(radians), bounds.CenterY + dx * Math.Sin(radians) + dy * Math.Cos(radians));
    }
    public static Rect DrawingBounds(WireframeElement e)
    {
        var points = new[] { Rotate(e.X,e.Y,e.Bounds,e.Rotation), Rotate(e.Bounds.Right,e.Y,e.Bounds,e.Rotation),
            Rotate(e.X,e.Y+ElementState.DrawingHeight(e),e.Bounds,e.Rotation), Rotate(e.Bounds.Right,e.Y+ElementState.DrawingHeight(e),e.Bounds,e.Rotation) };
        return new(points.Min(p=>p.X),points.Min(p=>p.Y),points.Max(p=>p.X)-points.Min(p=>p.X),points.Max(p=>p.Y)-points.Min(p=>p.Y));
    }
}
