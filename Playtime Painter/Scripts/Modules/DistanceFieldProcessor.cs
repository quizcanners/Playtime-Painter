/*
 * The script below was originally obtained from
 * SDF Toolkit Free by CATLIKE CODING (Can be found on the Asset Store)
 * The generator is based on the Anti-aliased Euclidean distance transform described by Stefan Gustavson and Robin Strand.
 * The algorithm it uses is an adapted version of Stefan Gustavson's code and falls under the permissive MIT License.
 * This means that you can bundle it with your commercial products.
 * The MIT license applies only to this class which was originally named SDFTextureGenerator.cs.
 *
 *
 * I modified it to work with Playtime Painter's TextureMeta class, but the underlying technique remains the same. 
 */

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizCannersUtilities;
using UnityEngine;



namespace PlaytimePainter {

    public static class DistanceFieldProcessor {
		
		private class Pixel {
            public float originalValue;
            public float distance;
			public Vector2 gradient;
			public int dX, dY;

            public void FlipOriginal() => originalValue = 1 - originalValue;

            public void ResetTransform()
            {
                dX = 0;
                dY = 0;
                if (originalValue <= 0f)
                    distance = 1000000f;
                else if (originalValue < 1f)
                    distance = ApproximateEdgeDelta(gradient.x, gradient.y, originalValue);
                else
                    distance = 0f;
            }

            public Pixel() { }

            public Pixel(float original)
            {
                originalValue = original > 0.5f ? 1f : 0f;
            }
        }
		
		private static int width, height;
		private static Pixel[,] pixels;
        private static TextureMeta destination;

        static void SetDestination(int x, int y, float value) {

            var col = destination.PixelUnSafe(x, y);
            col.r = value;
            col.g = value;
            col.b = value;

            destination.SetPixelUnSafe(x, y, col);
        }


        private static  void InitializePixels() {

            int x, y;

            for (y = 0; y < height; y++) {

                for (x = 0; x < width; x++)
                    pixels[x, y] = new Pixel(1f - destination.PixelUnSafe(x, y).grayscale);
            }
        } 

		public static IEnumerator Generate (
            TextureMeta image,
			float maxInside,
			float maxOutside,
			float postProcessDistance) {

			width = image.width;
			height = image.height;

            destination = image;

            if (image.Pixels == null) {
                Debug.LogError("Pixels are null");
                yield break;
            }

            pixels = new Pixel[width, height];
			int x, y;
			float scale;

            yield return QcAsync.CallAgain("Creating pixels");

            yield return QcAsync.CallAfter_Thread(() => InitializePixels(), "Creating pixels coroutine");

            yield return QcAsync.CallAgain("Filling max Inside");

            //INSIDE
            if (maxInside > 0f){
                
                for (var e = ComputeEdgeGradients(); e.MoveNext();)
                    yield return e.Current;
                
                for (var e = GenerateDistanceTransform(); e.MoveNext();)
                    yield return e.Current;

                if (postProcessDistance > 0f)
                    for (var e = PostProcess(postProcessDistance); e.MoveNext();)
                        yield return e.Current;


                yield return QcAsync.CallAgain("Setting Inside Pixels");

                scale = 1f / maxInside;
                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                        SetDestination(x, y, Mathf.Clamp01(pixels[x, y].distance * scale));
                    
                    yield return QcAsync.CallAgain("Inside Pixels {0}".F(y));
                }

            }

            yield return QcAsync.CallAgain("Filling max Outside");


            //OUTSIDE
            if (maxOutside > 0f)
            {

                yield return QcAsync.CallAfter_Thread(() => {

                    for (y = 0; y < height; y++)
                    for (x = 0; x < width; x++)
                        pixels[x, y].FlipOriginal();
                }, "Flipping pixels");
                

                for (var e = ComputeEdgeGradients(); e.MoveNext();)
                    yield return e.Current;

                for (var e = GenerateDistanceTransform(); e.MoveNext();)
                    yield return e.Current;

                if (postProcessDistance > 0f)
                    for (var e = PostProcess(postProcessDistance); e.MoveNext();)
                        yield return e.Current;

                scale = 1f / maxOutside;
				if(maxInside > 0f){
					for(y = 0; y < height; y++)
						for(x = 0; x < width; x++){
							float value = 0.5f + (destination.PixelUnSafe(x, y).r - 
                                                  Mathf.Clamp01(pixels[x, y].distance * scale)) * 0.5f;
                            SetDestination(x, y, value);
						}

                    yield return QcAsync.CallAgain("Setting Outside Pixels {0}/{1}".F(y, height));
                }
				else{
                    for (y = 0; y < height; y++) {
                        for (x = 0; x < width; x++) {
                            var value = Mathf.Clamp01(1f - pixels[x, y].distance * scale);
                            SetDestination(x, y, value);
                        }

                        yield return QcAsync.CallAgain("Setting Outside Pixels {0}/{1}".F(y, height));
                    }
                }
			}
			

			pixels = null;
		}
        
		private static IEnumerator ComputeEdgeGradients () {

            float sqrt2 = Mathf.Sqrt(2f);

            yield return QcAsync.CallAfter_Thread(() =>
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        var p = pixels[x, y];
                        if (p.originalValue > 0f && p.originalValue < 1f)
                        {
                            float g =
                                -pixels[x - 1, y - 1].originalValue
                                - pixels[x - 1, y + 1].originalValue
                                + pixels[x + 1, y - 1].originalValue
                                + pixels[x + 1, y + 1].originalValue;
                            p.gradient.x = g + (pixels[x + 1, y].originalValue - pixels[x - 1, y].originalValue) *
                                           sqrt2;
                            p.gradient.y = g + (pixels[x, y + 1].originalValue - pixels[x, y - 1].originalValue) *
                                           sqrt2;
                            p.gradient.Normalize();
                        }
                    }
                }
            }, "Edge grad A");


            yield return QcAsync.CallAfter_Thread(() =>
            {

                for (int y = 0; y < height; y++)
                {

                    int skip = ((y == 0) || (y == (height - 1))) ? 1 : (width - 1);

                    for (int x = 0; x < width; x += skip)
                    {
                        var p = pixels[x, y];
                        if (p.originalValue > 0f && p.originalValue < 1f)
                        {

                            float g =
                                -Pixels(x - 1, y - 1).originalValue
                                - Pixels(x - 1, y + 1).originalValue
                                + Pixels(x + 1, y - 1).originalValue
                                + Pixels(x + 1, y + 1).originalValue;
                            p.gradient.x = g + (Pixels(x + 1, y).originalValue - Pixels(x - 1, y).originalValue) *
                                           sqrt2;
                            p.gradient.y = g + (Pixels(x, y + 1).originalValue - Pixels(x, y - 1).originalValue) *
                                           sqrt2;
                            p.gradient.Normalize();
                        }
                    }
                }
            }, "Edge grad B");


        }

        private static Pixel Pixels(int x, int y) => pixels[(x + width) % width, (y + height) % height];
        
        private static float ApproximateEdgeDelta (float gx, float gy, float a) {

			if(gx == 0f || gy == 0f)
                return 0.5f - a;
			
			float length = Mathf.Sqrt(gx * gx + gy * gy);
			gx = gx / length;
			gy = gy / length;
			
			gx = Mathf.Abs(gx);
			gy = Mathf.Abs(gy);
			if(gx < gy){
				var temp = gx;
				gx = gy;
				gy = temp;
			}
			
			var a1 = 0.5f * gy / gx;

			if(a < a1)
                return 0.5f * (gx + gy) - Mathf.Sqrt(2f * gx * gy * a);
			
			if(a < (1f - a1))
                return (0.5f - a) * gx;
			
			return -0.5f * (gx + gy) + Mathf.Sqrt(2f * gx * gy * (1f - a));
		}

        private static void UpdateDistanceSafe(int x, int y, int sign = -1) {
            var p = pixels[x, y];
            if (p.distance > 0f) {
                UpdateDistanceSafe(p, x, y, sign, 0);
                UpdateDistanceSafe(p, x, y, sign, sign);
                UpdateDistanceSafe(p, x, y, 0, sign);
                UpdateDistanceSafe(p, x, y, -sign, sign);
            }
        }

        private static void UpdateDistanceSafe(Pixel p, int x, int y, int oX, int oY)
        {
            var neighbor = Pixels(x + oX, y + oY);
            var closest = Pixels(x + oX - neighbor.dX, y + oY - neighbor.dY);

            if (closest.originalValue == 0f || closest == p)
                return;

            int dX = neighbor.dX - oX;
            int dY = neighbor.dY - oY;
            float distance = Mathf.Sqrt(dX * dX + dY * dY) + ApproximateEdgeDelta(dX, dY, closest.originalValue);
            if (distance < p.distance)
            {
                p.distance = distance;
                p.dX = dX;
                p.dY = dY;
            }
        }
		
		private static IEnumerator GenerateDistanceTransform () {

			int x = 0, y = 0;
			Pixel p;

            yield return QcAsync.CallAfter_Thread(() =>
            {

                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                        pixels[x, y].ResetTransform();

                }
            }, "Reseting Dist Tf");

            yield return QcAsync.CallAfter_Thread(() =>
            {

                for (y = 0; y < height; y++) {

                    var dy = (y + height) % height;

                    for (x = 0; x < width; x++)
                        UpdateDistanceSafe(x, dy);

                    for (x = width - 1; x >= 0; x--) {
                        p = pixels[x, dy];
                        if (p.distance > 0f)
                            UpdateDistanceSafe(p, x, dy, 1, 0);
                    }

                }
            }, "Dist down");

            yield return QcAsync.CallAfter_Thread(() =>
            {

                for (y = height - 1; y >= -8; y--)
                {

                    var dy = (y + height) % height;

                    for (x = width - 1; x >= 0; x--)
                        UpdateDistanceSafe(x, dy, 1);

                    for (x = 0; x < width; x++)
                    {
                        p = pixels[x, dy];
                        if (p.distance > 0f)
                            UpdateDistanceSafe(p, x, dy, -1, 0);
                    }

                }
            }, "Dist up");

            yield return QcAsync.CallAfter_Thread(() => {
                
                for (y = -2; y < 7; y++) {

                    var dy = (y + height) % height;

                    for (x = 0; x < width; x++)
                        UpdateDistanceSafe(x, dy);
                }

                for (y = 6; y > -3; y--) {

                    var dy = (y + height) % height;

                    for (x = width - 1; x >= 0; x--)
                        UpdateDistanceSafe(x, dy, 1);
                }
            }, "Dist edge");

        }
		
		private static IEnumerator PostProcess (float maxDistance)
        {

            yield return QcAsync.CallAfter_Thread(() =>
            {

                for (int y = 0; y < height; y++)
                {

                    for (int x = 0; x < width; x++)
                    {
                        var p = pixels[x, y];
                        if ((p.dX == 0 && p.dY == 0) || p.distance >= maxDistance)
                            continue;

                        float
                            dX = p.dX,
                            dY = p.dY;
                        var closest = Pixels(x - p.dX, y - p.dY);
                        var g = closest.gradient;

                        if (g.x == 0f && g.y == 0f)
                            continue;

                        float df = ApproximateEdgeDelta(g.x, g.y, closest.originalValue);
                        float t = dY * g.x - dX * g.y;
                        float u = -df * g.x + t * g.y;
                        float v = -df * g.y - t * g.x;

                        if (Mathf.Abs(u) <= 0.5f && Mathf.Abs(v) <= 0.5f)
                            p.distance = Mathf.Sqrt((dX + u) * (dX + u) + (dY + v) * (dY + v));

                    }
                }
            }, "Postprocess");
        }
		
	}
}
