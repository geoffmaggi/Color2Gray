#Color 2 Gray
  
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
 * =========================================