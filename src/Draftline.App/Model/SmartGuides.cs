namespace Draftline.App.Model;
public sealed record GuideSnap(double X, double Y, double? Vertical, double? Horizontal);
/// <summary>Nearby edges and centers, with one common translation for a moving selection.</summary>
public static class SmartGuides
{
    public static GuideSnap Snap(Rect moving, IEnumerable<Rect> targets, double width, double height, double tolerance = 6)
    {
        var all = targets.Append(new Rect(0, 0, width, height)).ToList();
        (double Delta, double? Guide) Closest(double[] source, IEnumerable<double> values)
        {
            var best = tolerance + .001;
            var delta = 0d;
            double? guide = null;
            foreach (var target in values)
                foreach (var edge in source)
                    if (Math.Abs(target - edge) < best)
                    {
                        best = Math.Abs(target - edge);
                        delta = target - edge;
                        guide = target;
                    }

            return (delta, guide);
        }

        var x = Closest(new[] { moving.X, moving.CenterX, moving.Right }, all.SelectMany(r => new[] { r.X, r.CenterX, r.Right }));
        var y = Closest(new[] { moving.Y, moving.CenterY, moving.Bottom }, all.SelectMany(r => new[] { r.Y, r.CenterY, r.Bottom }));
        return new(x.Delta, y.Delta, x.Guide, y.Guide);
    }
}
