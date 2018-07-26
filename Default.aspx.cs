using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

/*=========================================
  
  Based off Color2Gray: Salience-Preserving Color Removal. Amy A. Gooch, Sven C. Olsen, Jack Tumblin, and Bruce Gooch. SIGGRAPH 05
  http://www.cs.northwestern.edu/~ago820/color2gray/color2gray.pdf
 
 * Supports .jpg and .png images in RGB ColorSpace

 * To Do's:
 *  - Upload Images*
 *  - Convert Images From RGB to CIE LaB*
 *    - RGB -> XYZ //Reduced to one step
 *    - XYZ -> LMS //Reduced to one step
 *    - RGB -> LMS*
 *    - LMS -> LaB*
 *    
 *  - Compute Target Differences using luminance and chrominance differences
 *  
 *  - Use a Least Square Optimization to selectively modulate the source luminance differences (VIA iterations)
 * 
 *  - Convert Images from CIE LaB to RGB to be displayed*
 *    - LaB -> LMS*
 *    - LMS -> RGB*
 * 
 * Extras:
 *  - Multiple Gray Scale algorithms
 *  - Recoloring for individuals with color blindness :)
 *    - Based off the work of Dalton J(http://www.daltonize.org)
 *    
 *  ========== Bug fixes: ==================
 *    - NaN or out of range values in images* (Fixed)
 * ========================================= */

namespace Color2Gray {
  public partial class Default : System.Web.UI.Page {

    //Used to manipulate the JPEG bitmaps
    protected struct col {
      public float R; //L L
      public float G; //M a
      public float B; //S b
    }

    protected void Page_Load(object sender, EventArgs e) {
      debug.Text = "";
    }

    //Used to toggle the extra menu options
    protected void showOptions(object sender, EventArgs e) {
      switch (taskDDL.SelectedValue) {
        case "color2Gray":
        case "color2Color":
          C2GPanel.Visible = true;
          CBPanel.Visible = false;
          break;
        case "color2CC":
          C2GPanel.Visible = true;
          CBPanel.Visible = true;
          break;
        default:
          C2GPanel.Visible = false;
          CBPanel.Visible = false;;
          break;
      }

    }

    protected void run(object sender, EventArgs e) {
      if (sourceFU.HasFile) {
        //source(RGB) -> sourceEdit(Lab)
        Bitmap source = new Bitmap(sourceFU.PostedFile.InputStream);

        //RGB
        col[,] sourceEdit = BitmapToCol(source);
        Bitmap result = colToBitmap(sourceEdit);
        showImage(result); //Show original image

        switch (taskDDL.SelectedValue) { //img in RGB format at end
          case "grayscaleAverage": {
              RGBtoGrayscaleAvg(ref sourceEdit);
              break;
            }
          case "grayscaleBT601": {
              RGBtoGrayscaleBT601(ref sourceEdit);
              break;
            }
          case "grayscaleDesaturation": {
              RGBtoGrayscaleDesat(ref sourceEdit);
              break;
            }
          case "grayscaleLab": {
              RGBtoLMS(ref sourceEdit);
              LMStoLab(ref sourceEdit);

              LabtoGray(ref sourceEdit);

              LabtoLMS(ref sourceEdit);
              LMStoRGB(ref sourceEdit);
              break;
            }
          case "color2Gray": {
              RGBtoLMS(ref sourceEdit);
              LMStoLab(ref sourceEdit);

              int r;
              if (!int.TryParse(RadiusTB.Text, out r)) r = 5;
              float alpha;
              if (!float.TryParse(AlphaTB.Text, out alpha)) alpha = 10;
              float theta;
              if (!float.TryParse(ThetaTB.Text, out theta)) theta = 45;
              theta *= (float)(3.1459 / 180);
              int iters;
              if (!int.TryParse(ItersTB.Text, out iters)) iters = 10;
              col[,] orig = (col[,])sourceEdit.Clone();

              float[,] distance = calcDistance(sourceEdit, r, theta, alpha);

              solve(ref sourceEdit, ref distance, r, iters);
              postSolve(ref sourceEdit, ref orig);

              LabtoGray(ref sourceEdit); //0 out the a,b channels
              LabtoLMS(ref sourceEdit);
              LMStoRGB(ref sourceEdit);
              break;
            }
          case "color2Color": {
              RGBtoLMS(ref sourceEdit);
              LMStoLab(ref sourceEdit);

              int r;
              if (!int.TryParse(RadiusTB.Text, out r)) r = 5;
              float alpha;
              if (!float.TryParse(AlphaTB.Text, out alpha)) alpha = 10;
              float theta;
              if (!float.TryParse(ThetaTB.Text, out theta)) theta = 45;
              theta *= (float)(3.1459 / 180);
              int iters;
              if (!int.TryParse(ItersTB.Text, out iters)) iters = 10;
              col[,] orig = (col[,])sourceEdit.Clone();

              float[,] distance = calcDistance(sourceEdit, r, theta, alpha);

              solve(ref sourceEdit, ref distance, r, iters);
              postSolve(ref sourceEdit, ref orig);

              LabtoLMS(ref sourceEdit);
              LMStoRGB(ref sourceEdit);
              break;
            }
          case "color2CC": {
              RGBtoLMS(ref sourceEdit);
              LMStoLab(ref sourceEdit);

              int r;
              if (!int.TryParse(RadiusTB.Text, out r)) r = 5;
              float alpha;
              if (!float.TryParse(AlphaTB.Text, out alpha)) alpha = 10;
              float theta;
              if (!float.TryParse(ThetaTB.Text, out theta)) theta = 45;
              theta *= (float)(3.1459 / 180);
              int iters;
              if (!int.TryParse(ItersTB.Text, out iters)) iters = 10;
              col[,] orig = (col[,])sourceEdit.Clone();
              int cb;
              if (!int.TryParse(CBDDL.SelectedValue, out cb)) cb = 1;

              float[,] distance = calcDistance(sourceEdit, r, theta, alpha);

              solve(ref sourceEdit, ref distance, r, iters);
              postSolve(ref sourceEdit, ref orig);

              LabtoLMS(ref sourceEdit);

              col[,] err = (col[,])sourceEdit.Clone();

              LMStoRGB(ref sourceEdit);

              LMStoCB(ref err, cb); //calculate the CB pixels
              LMStoRGB(ref err);
              result = colToBitmap(err);
              showImage(result);

              for (int i = 0; i < err.GetLength(0); i++) { //remove them from the image
                for (int j = 0; j < err.GetLength(1); j++) {
                  err[i, j].R -= sourceEdit[i, j].R;
                  err[i, j].G -= sourceEdit[i, j].B;
                  err[i, j].B -= sourceEdit[i, j].G;
                }
              }
              result = colToBitmap(err);
              showImage(result);
              CBCorrect(ref err); //correct them
              result = colToBitmap(err);
              showImage(result);

              for (int i = 0; i < err.GetLength(0); i++) { //add them back in
                for (int j = 0; j < err.GetLength(1); j++) {
                  sourceEdit[i, j].R += err[i, j].R;
                  sourceEdit[i, j].G += err[i, j].B;
                  sourceEdit[i, j].B += err[i, j].G;
                }
              }
              
              break;
            }
        }

        result = colToBitmap(sourceEdit);
        showImage(result);
      }
      else {
        debug.Text = "You forgot to select an image";
      }
      return;
    }

    protected float[,] calcDistance(col[,] img, int r, float theta, float alpha) {
      int w = img.GetLength(0);
      int h = img.GetLength(1);
      
      float[,] ret = new float[w, h];
      Array.Clear(ret, 0, ret.Length); //0 it out

      for (int i = 0; i < w; i++) {
        for (int j = 0; j < h; j++) {
          for (int x = i - r; x <= i + r; x++) {
            if (x < 0 || x >= w) continue;
            for (int y = j - r; y <= j + r; y++) {
              if (y < 0 || y >= h) continue;
              float delta = calcDelta(img[i, j], img[x, y], theta, alpha);
              ret[i, j] += delta;
              ret[x, y] -= delta;
            }
          }
        }
      }
      return ret;
    }

    protected float calcDelta(col a, col b, float theta, float alpha) {
      float dL = a.R - b.R;
      float v = (float)Math.Sqrt((a.G - b.G) * (a.G - b.G) + (a.B - b.B) * (a.B - b.B));
      float dC = crunch(v, alpha);

      if (Math.Abs(dL) > dC) return dL;
      return dC * ((Math.Cos(theta) * (a.G - b.G) + Math.Sin(theta) * (a.B - b.B)) > 0 ? 1 : -1);
    }

    protected float crunch(float chrom_dist, float alphaL) {
      return alphaL == 0 ? (float)0 : (float)(alphaL * Math.Tanh(chrom_dist / alphaL));
    }

    protected void solve(ref col[,] img, ref float[,] d, int r, int iters) {
      int w = img.GetLength(0);
      int h = img.GetLength(1);

      for (int k = 0; k < iters; k++) {
        for (int i = 0; i < w; i++) {
          for (int j = 0; j < h; j++) {
            float sum = 0;
            int count = 0;
            for (int x = i - r; x <= i + r; x++) {
              if (x < 0 || x >= w) continue;
              for (int y = j - r; y <= j + r; y++) {
                if (y < 0 || y >= h) continue;
                sum += img[x, y].R;
                count++;
              }
            }
            img[i, j].R = (float)(d[i, j] + sum) / count;
          }
        }
      }
    }

    protected void postSolve(ref col[,] img, ref col[,] source) {
      float error = 0;
      int w = img.GetLength(0);
      int h = img.GetLength(1);

      for (int i = 0; i < w; i++)
        for (int j = 0; j < h; j++)
          error += img[i,j].R - source[i,j].R;
      error /= w*h;
      for (int i = 0; i < w; i++)
        for (int j = 0; j < h; j++)
          img[i,j].R -= error;
    }
    
    protected void LabtoGray(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {

          //Keep L
          img[i, j].G = 0; //a
          img[i, j].B = 0; //b
        }
      }
    }

    protected void LabRecolor(ref col[,] img, ref float[,] d) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {

          //Keep L
          img[i, j].G -= (float)Math.Sqrt(d[i,j]); //a
          img[i, j].B += (float)Math.Sqrt(d[i, j]); //b
        }
      }
    }

    protected void LMStoCB(ref col[,] img, int type) {
      float[,] toCB;
      switch (type) {
        case 1: //Deuteranopia: green weakness
          toCB = new float[3, 3]
                { {(float)1.0, (float)0.0, (float)0.0},
                  {(float)0.494207, (float)0.0, (float)1.24827},
                  {(float)0.0, (float)0.0, (float)1.0}
                };
          break;
        case 2: //Protanopia: red weakness
          toCB = new float[3, 3]
                { {(float)0.0, (float)2.02344, (float)-2.52581},
                  {(float)0.0, (float)1.0, (float)0.0},
                  {(float)0.0, (float)0.0, (float)1.0}
                };
          break;
        case 3: //Tritanopia: blue weakness
          toCB = new float[3, 3]
                { {(float)1.0, (float)0.0, (float)0.0},
                  {(float)0.0, (float)1.0, (float)0.0},
                  {(float)-0.395913, (float)0.801109, (float)0.0}
                };
          break;
        default:
          return;//do nothing
      }

      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float[] LMS = new float[3] { (img[i, j]).R, (img[i, j]).G, (img[i, j]).B };

          float[] CB = matMult(LMS, toCB);

          img[i, j].R = CB[0];
          img[i, j].G = CB[1];
          img[i, j].B = CB[2];
        }
      }
    }

    protected void CBCorrect(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float[] RGB = new float[3] { img[i, j].R, img[i, j].G, img[i, j].B };
          float[,] toRGB = new float[3, 3]
          { {(float)0.0, (float)0.0, (float)0.0},
            {(float)0.7, (float)1.0, (float)0.0},
            {(float)0.7, (float)0.0, (float)1.0}
          };

          float[] Corrected = matMult(RGB, toRGB);
          img[i, j].R = Corrected[0];
          img[i, j].G = Corrected[1];
          img[i, j].B = Corrected[2];
        }
      }
    }

    protected void RGBtoGrayscaleBT601(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float gray = (float)(img[i, j].R * 0.299 + img[i, j].G * 0.587 + img[i, j].B* 0.114);


          img[i, j].R = gray;
          img[i, j].G = gray;
          img[i, j].B = gray;
        }
      }
    }

    protected void RGBtoGrayscaleAvg(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float gray = (float)((img[i, j].R + img[i, j].G + img[i, j].B) / 3);


          img[i, j].R = gray;
          img[i, j].G = gray;
          img[i, j].B = gray;
        }
      }
    }

    protected void RGBtoGrayscaleDesat(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float gray = (float)((Math.Max(img[i, j].R, Math.Max(img[i, j].G, img[i, j].B))
                              + Math.Min(img[i, j].R, Math.Min(img[i, j].G, img[i, j].B))) / 2.0);


          img[i, j].R = gray;
          img[i, j].G = gray;
          img[i, j].B = gray;
        }
      }
    }

    protected col[,] BitmapToCol(Bitmap img) {
      col[,] ret = new col[img.Width, img.Height];
      for (int i = 0; i < img.Width; i++) {
        for (int j = 0; j < img.Height; j++) {
          Color pixel = img.GetPixel(i, j);
          (ret[i, j]).R = Convert.ToInt16(pixel.R);
          (ret[i, j]).G = Convert.ToInt16(pixel.G);
          (ret[i, j]).B = Convert.ToInt16(pixel.B);
        }
      }
      return ret;
    }

    protected Bitmap colToBitmap(col[,] img) {
      Bitmap ret = new Bitmap(img.GetLength(0), img.GetLength(1));
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          img[i, j].R = img[i, j].R != img[i, j].R ? 0 : img[i, j].R > 255 ? 255 : img[i, j].R < 0 ? 0 : img[i, j].R;
          img[i, j].G = img[i, j].G != img[i, j].G ? 0 : img[i, j].G > 255 ? 255 : img[i, j].G < 0 ? 0 : img[i, j].G;
          img[i, j].B = img[i, j].B != img[i, j].B ? 0 : img[i, j].B > 255 ? 255 : img[i, j].B < 0 ? 0 : img[i, j].B;

          int red = Convert.ToInt32(img[i, j].R);
          int green = Convert.ToInt32(img[i, j].G);
          int blue = Convert.ToInt32(img[i, j].B);

          Color px = Color.FromArgb(red, green, blue);
          ret.SetPixel(i, j, px);
        }
      }
      return ret;
    }

    protected void RGBtoLMS(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float[] RGB = new float[3] { (img[i, j]).R, (img[i, j]).G, (img[i, j]).B };
          float[,] toLMS = new float[3, 3]
          { {(float)0.3811, (float)0.5783, (float)0.0402},
            {(float)0.1967, (float)0.7244, (float)0.0782},
            {(float)0.0241, (float)0.1288, (float)0.8444}
          };

          float[] LMS = matMult(RGB, toLMS);

          img[i, j].R = LMS[0];
          img[i, j].G = LMS[1];
          img[i, j].B = LMS[2];
        }
      }
    }

    protected void LMStoLab(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float[] LMS = new float[3] {img[i, j].R == 0 ? 0 : (float)(Math.Log10((img[i, j]).R)),
                                      img[i, j].G == 0 ? 0 : (float)Math.Log10((img[i, j]).G),
                                      img[i, j].B == 0 ? 0 : (float)Math.Log10((img[i, j]).B) };
          float[,] toLab1 = new float[3, 3]
          { {(float)1, (float)1, (float)1},
            {(float)1, (float)1, (float)-2},
            {(float)1, (float)-1, (float)0.0}
          };
          float[,] toLab2 = new float[3, 3]
          { {(float)(1/Math.Sqrt(3)), (float)0.0, (float)0.0},
            {(float)0.0, (float)(1/Math.Sqrt(6)), (float)0.0},
            {(float)0.0, (float)0.0, (float)(1/Math.Sqrt(2))}
          };

          float[] Lab = matMult(LMS, toLab1);
          Lab = matMult(Lab, toLab2);

          img[i, j].R = Lab[0];
          img[i, j].G = Lab[1];
          img[i, j].B = Lab[2];
        }
      }
    }

    protected void LabtoLMS(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float[] Lab = new float[3] { (img[i, j]).R, (img[i, j]).G, (img[i, j]).B };
          float[,] toLMS1 = new float[3, 3]
          { {(float)(Math.Sqrt(3)/3), (float)0.0, (float)0.0},
            {(float)0.0, (float)(Math.Sqrt(6)/6), (float)0.0},
            {(float)0.0, (float)0.0, (float)(Math.Sqrt(2)/2)}
          };
          float[,] toLMS2 = new float[3, 3]
          { {(float)1, (float)1, (float)1},
            {(float)1, (float)1, (float)-1},
            {(float)1, (float)-2, (float)0.0}
          };

          float[] LMS = matMult(Lab, toLMS1);
          LMS = matMult(LMS, toLMS2);

          img[i, j].R = (float)Math.Pow(10, LMS[0]);
          img[i, j].G = (float)Math.Pow(10, LMS[1]);
          img[i, j].B = (float)Math.Pow(10, LMS[2]);
        }
      }
    }

    protected void LMStoRGB(ref col[,] img) {
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          float[] LMS = new float[3] { img[i, j].R, img[i, j].G, img[i, j].B };
          float[,] toRGB = new float[3, 3]
          { {(float)4.4679, (float)-3.5873, (float)0.1193},
            {(float)-1.2186, (float)2.3809, (float)-0.1624},
            {(float)0.0497, (float)-0.2439, (float)1.2045}
          };

          float[] RGB = matMult(LMS, toRGB);
          img[i, j].R = RGB[0];
          img[i, j].G = RGB[1];
          img[i, j].B = RGB[2];
        }
      }
    }

    protected col calcMean(ref col[,] img) {
      col ret = new col();
      ret.R = 0;
      ret.G = 0;
      ret.B = 0;

      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          if (img[i, j].R == img[i, j].R)
            ret.R += img[i, j].R;
          if (img[i, j].G == img[i, j].G)
            ret.G += img[i, j].G;
          if (img[i, j].B == img[i, j].B)
            ret.B += img[i, j].B;
        }
      }

      ret.R /= (img.GetLength(0) * img.GetLength(1));
      ret.G /= (img.GetLength(0) * img.GetLength(1));
      ret.B /= (img.GetLength(0) * img.GetLength(1));

      return ret;
    }

    protected col calcStdDiv(ref col[,] img) {
      col ret = new col();
      ret.R = 0;
      ret.G = 0;
      ret.B = 0;

      //1. Calc Mean
      col mean = calcMean(ref img);

      //2. subtract the Mean and square the result.
      //3. Then work out the mean of those squared differences.
      for (int i = 0; i < img.GetLength(0); i++) {
        for (int j = 0; j < img.GetLength(1); j++) {
          if (img[i, j].R == img[i, j].R)
            ret.R += (img[i, j].R - mean.R) * (img[i, j].R - mean.R);
          if (img[i, j].G == img[i, j].G)
            ret.G += (img[i, j].G - mean.G) * (img[i, j].G - mean.G);
          if (img[i, j].B == img[i, j].B)
            ret.B += (img[i, j].B - mean.B) * (img[i, j].B - mean.B);
        }
      }

      ret.R /= (img.GetLength(0) * img.GetLength(1));
      ret.G /= (img.GetLength(0) * img.GetLength(1));
      ret.B /= (img.GetLength(0) * img.GetLength(1));

      //4. Take the square root of that and we are done!
      ret.R = (float)Math.Sqrt(ret.R);
      ret.G = (float)Math.Sqrt(ret.G);
      ret.B = (float)Math.Sqrt(ret.B);


      return ret;
    }

    protected float[] matMult(float[] a, float[,] b) {
      float[] ret = new float[3] { 0, 0, 0 };

      for (int i = 0; i <= 2; i++) {
        for (int j = 0; j <= 2; j++) {
          ret[i] += a[j] * b[i, j];
        }
      }
      return ret;
    }

    //Addes the image to ImageUP
    protected void showImage(Bitmap img) {
      MemoryStream ms = new MemoryStream();
      img.Save(ms, ImageFormat.Jpeg);
      var base64Data = Convert.ToBase64String(ms.ToArray());

      System.Web.UI.WebControls.Image newImg = new System.Web.UI.WebControls.Image();
      newImg.ImageUrl = "data:image/jpg;base64," + base64Data;
      newImg.Visible = true;
      imageUP.ContentTemplateContainer.Controls.Add(newImg);
    }

    //Clears the form and the Image Panel
    protected void clear(object sender, EventArgs e) {
      Response.Redirect(Request.RawUrl); //Just refresh
      
      //sourceFU.Attributes.Clear();
      //imageUP.ContentTemplateContainer.Controls.Clear();
    }
  }
}