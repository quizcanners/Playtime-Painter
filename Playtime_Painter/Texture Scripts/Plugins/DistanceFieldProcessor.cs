/*
 * The script below was originally obtained from
 * SDF Toolkit Free by CATLIKE CODING (Can be found on the Asset Store)
 * The generator is based on the Anti-aliased Euclidean distance transform described by Stefan Gustavson and Robin Strand.
 * The algorithm it uses is an adapted version of Stefan Gustavson's code and falls under the permissive MIT License.
 * This means that you can bundle it with your commercial products.
 * The MIT license applies only to this class which was originally named SDFTextureGenerator.cs.
 *
 *
 * I modified it to work with Playtime Painter's ImageMeta class, but the underlying technique remains the same. 
 */

using UnityEngine;

namespace Playtime_Painter {
    
    public static class DistanceFieldProcessor
    {
		
		private class Pixel {
            public float originalValue;
            public float distance;
			public Vector2 gradient;
			public int dX, dY;
		}
		
		private static int width, height;
		private static Pixel[,] pixels;
        private static ImageMeta img;

        /// <param name="maxInside">
        /// Maximum pixel distance measured inside the edge, resulting in an alpha value of 1.
        /// If set to or below 0, everything inside will have an alpha value of 1.
        /// </param>
        /// <param name="maxOutside">
        /// Maximum pixel distance measured outside the edge, resulting in an alpha value of 0.
        /// If set to or below 0, everything outside will have an alpha value of 0.
        /// </param>
        /// <param name="postProcessDistance">
        /// Pixel distance from the edge within which pixels will be post-processed using the edge gradient.
        /// </param>

        static void SetColor(int x, int y, float value, float scale) {

            var col = img.PixelUnSafe(x, y);
            col.r = value;
            col.g = value;
            col.b = value;

            img.SetPixelUnSafe(x, y, col);
        }

		public static void Generate (
            ImageMeta image,
			float maxInside,
			float maxOutside,
			float postProcessDistance) {

			width = image.width;
			height = image.height;

            img = image;

            pixels = new Pixel[width, height];
			int x, y;
			float scale;

			//Color c = Color.black;

			for(y = 0; y < height; y++)
				for(x = 0; x < width; x++)
					pixels[x, y] = new Pixel();
				
			if(maxInside > 0f){
				for(y = 0; y < height; y++)
					for(x = 0; x < width; x++)
						pixels[x, y].originalValue = 1f - image.PixelUnSafe(x, y).grayscale;
					
				ComputeEdgeGradients();
				GenerateDistanceTransform();
				if(postProcessDistance > 0f)
					PostProcess(postProcessDistance);
				
				scale = 1f / maxInside;

				for(y = 0; y < height; y++)
					for(x = 0; x < width; x++){
						//c.r = Mathf.Clamp01(pixels[x, y].distance * scale);
                       // image.SetPixelUnSafe(x, y, c);

                        SetColor(x, y, Mathf.Clamp01(pixels[x, y].distance * scale), scale);


                    }
				
			}

			if(maxOutside > 0f){
				for(y = 0; y < height; y++)
					for(x = 0; x < width; x++)
						pixels[x, y].originalValue = image.PixelUnSafe(x, y).r;
					
				ComputeEdgeGradients();
				GenerateDistanceTransform();
				if(postProcessDistance > 0f)
					PostProcess(postProcessDistance);
				
				scale = 1f / maxOutside;
				if(maxInside > 0f){
					for(y = 0; y < height; y++)
						for(x = 0; x < width; x++){
							float value = 0.5f + (image.PixelUnSafe(x, y).r - Mathf.Clamp01(pixels[x, y].distance * scale)) * 0.5f;
                            SetColor(x, y, value, scale);
                            //image.SetPixelUnSafe(x, y, c);
						}
				}
				else{
					for(y = 0; y < height; y++)
						for(x = 0; x < width; x++){
							var value = Mathf.Clamp01(1f - pixels[x, y].distance * scale);
                            //image.SetPixelUnSafe(x, y, c);
                            SetColor(x, y, value, scale);
                        }
					
				}
			}
			

			pixels = null;
		}
		
		private static void ComputeEdgeGradients () {
			float sqrt2 = Mathf.Sqrt(2f);
			for(int y = 1; y < height - 1; y++) 
				for(int x = 1; x < width - 1; x++){
					var p = pixels[x, y];
					if(p.originalValue > 0f && p.originalValue < 1f){
						float g =
							- pixels[x - 1, y - 1].originalValue
							- pixels[x - 1, y + 1].originalValue
							+ pixels[x + 1, y - 1].originalValue
							+ pixels[x + 1, y + 1].originalValue;
						p.gradient.x = g + (pixels[x + 1, y].originalValue - pixels[x - 1, y].originalValue) * sqrt2;
						p.gradient.y = g + (pixels[x, y + 1].originalValue - pixels[x, y - 1].originalValue) * sqrt2;
						p.gradient.Normalize();
					}
				
			}
		}
		
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
		
		private static void UpdateDistance (this Pixel p, int x, int y, int oX, int oY) {
			var neighbor = pixels[x + oX, y + oY];
			var closest = pixels[x + oX - neighbor.dX, y + oY - neighbor.dY];
			
			if(closest.originalValue == 0f || closest == p)
                return;
			
			int dX = neighbor.dX - oX;
			int dY = neighbor.dY - oY;
			float distance = Mathf.Sqrt(dX * dX + dY * dY) + ApproximateEdgeDelta(dX, dY, closest.originalValue);
			if(distance < p.distance){
				p.distance = distance;
				p.dX = dX;
				p.dY = dY;
			}
		}
		
		private static void GenerateDistanceTransform () {

			int x, y;
			Pixel p;
			
			for(y = 0; y < height; y++){ 
				for(x = 0; x < width; x++){
					p = pixels[x, y];
					p.dX = 0;
					p.dY = 0;
					if(p.originalValue <= 0f)
						p.distance = 1000000f;
					else if (p.originalValue < 1f)
						p.distance = ApproximateEdgeDelta(p.gradient.x, p.gradient.y, p.originalValue);
					else
						p.distance = 0f;
					
				}
			}

            var preWidth = width - 1;

			for(y = 1; y < height; y++){

				p = pixels[0, y];
				if(p.distance > 0f){
					p.UpdateDistance( 0, y, 0, -1);
					p.UpdateDistance( 0, y, 1, -1);
				}

				for(x = 1; x < preWidth; x++){
					p = pixels[x, y];
					if(p.distance > 0f){
						p.UpdateDistance( x, y, -1, 0);
						p.UpdateDistance( x, y, -1, -1);
						p.UpdateDistance( x, y, 0, -1);
						p.UpdateDistance( x, y, 1, -1);
					}
				}

				p = pixels[preWidth, y];
				if(p.distance > 0f){
					p.UpdateDistance(preWidth, y, -1, 0);
					p.UpdateDistance(preWidth, y, -1, -1);
					p.UpdateDistance(preWidth, y, 0, -1);
				}

				for(x = width - 2; x >= 0; x--) {
					p = pixels[x, y];
					if(p.distance > 0f)
						p.UpdateDistance( x, y, 1, 0);
				}			
			}

			for(y = height - 2; y >= 0; y--){

				p = pixels[preWidth, y];
				if(p.distance > 0f){
					p.UpdateDistance(preWidth, y, 0, 1);
					p.UpdateDistance(preWidth, y, -1, 1);
				}

				for(x = width - 2; x > 0; x--){
					p = pixels[x, y];
					if(p.distance > 0f){
						p.UpdateDistance( x, y, 1, 0);
						p.UpdateDistance( x, y, 1, 1);
						p.UpdateDistance( x, y, 0, 1);
						p.UpdateDistance( x, y, -1, 1);
					}
				}

				p = pixels[0, y];
				if(p.distance > 0f){
					p.UpdateDistance( 0, y, 1, 0);
					p.UpdateDistance( 0, y, 1, 1);
					p.UpdateDistance( 0, y, 0, 1);
				}

				for(x = 1; x < width; x++){
					p = pixels[x, y];
					if(p.distance > 0f)
						p.UpdateDistance( x, y, -1, 0);
				}
			}
		}
		
		private static void PostProcess (float maxDistance) {

			for(int y = 0; y < height; y++)
				for(int x = 0; x < width; x++){
					var p = pixels[x, y];
					if((p.dX == 0 && p.dY == 0) || p.distance >= maxDistance)
                        continue;
					
					float
						dX = p.dX,
						dY = p.dY;
					var closest = pixels[x - p.dX, y - p.dY];
					var g = closest.gradient;
					
					if(g.x == 0f && g.y == 0f)
						continue;
					
					float df = ApproximateEdgeDelta(g.x, g.y, closest.originalValue);
					float t = dY * g.x - dX * g.y;
					float u = -df * g.x + t * g.y;
					float v = -df * g.y - t * g.x;
					
					if(Mathf.Abs(u) <= 0.5f && Mathf.Abs(v) <= 0.5f)
						p.distance = Mathf.Sqrt((dX + u) * (dX + u) + (dY + v) * (dY + v));
					
				}
		}
		
	}
}
