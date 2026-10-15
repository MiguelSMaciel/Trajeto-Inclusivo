using NUnit.Framework;
using UnityEngine;

namespace TrajetoInclusivo.Tests
{
    public sealed class LandmarkNavigationTests
    {
        [TestCase(0,0)][TestCase(300,200)][TestCase(600,400)][TestCase(-2000,-2000)][TestCase(2000,2000)]
        public void NearbyLabelsStaySeparatedAndWithinTheirMapRegion(float x,float y)
        {
            var area=new Rect(0,0,600,400);var labels=LandmarkLabelLayout.Arrange(area,new[]{new Vector2(x,y),new Vector2(x+1,y),new Vector2(x,y+1)});
            foreach(var label in labels)
            {Assert.That(label.xMin,Is.GreaterThanOrEqualTo(area.xMin));Assert.That(label.xMax,Is.LessThanOrEqualTo(area.xMax));Assert.That(label.yMin,Is.GreaterThanOrEqualTo(area.yMin));Assert.That(label.yMax,Is.LessThanOrEqualTo(area.yMax));}
            for(int i=0;i<labels.Length;i++)for(int j=i+1;j<labels.Length;j++)Assert.That(labels[i].Overlaps(labels[j]),Is.False);
        }
    }
}
