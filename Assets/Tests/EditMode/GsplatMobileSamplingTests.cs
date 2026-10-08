using Gsplat;
using NUnit.Framework;
using UnityEngine;

public sealed class GsplatMobileSamplingTests
{
    [Test]
    public void SpatialSamplingKeepsCoverageAndExactBudget()
    {
        Vector3[] positions =
        {
            Vector3.zero, Vector3.right * 0.1f, Vector3.right * 0.2f,
            Vector3.right * 10f, Vector3.right * 10.1f, Vector3.right * 10.2f
        };
        Vector3[] scales = { Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one };
        Vector4[] colors = { Vector4.one, Vector4.one, Vector4.one, Vector4.one, Vector4.one, Vector4.one };

        int[] indices = GsplatRenderer.CreateSpatialSampleIndices(
            positions, scales, colors, 4, new Bounds(Vector3.right * 5.1f, Vector3.right * 10.2f));

        Assert.That(indices, Has.Length.EqualTo(4));
        Assert.That(indices, Is.Ordered.And.Unique);
        Assert.That(indices, Has.Some.LessThan(3));
        Assert.That(indices, Has.Some.GreaterThanOrEqualTo(3));

        int[] singleIndex = GsplatRenderer.CreateSpatialSampleIndices(
            positions, scales, colors, 1, new Bounds(Vector3.right * 5.1f, Vector3.right * 10.2f));
        Assert.That(singleIndex, Has.Length.EqualTo(1));
    }
}
