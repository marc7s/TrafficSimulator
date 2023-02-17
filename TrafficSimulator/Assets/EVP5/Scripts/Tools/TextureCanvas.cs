//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;

namespace EVP
{

public class TextureCanvas
	{
	// Texture

	Texture2D m_texture;
	int m_pixelsWd;
	int m_pixelsHt;
	bool m_dirty;

	private Color32[] m_pixels;
	private Color32[] m_buffer;

	// Canvas rect

	Rect m_canvasRect;
	float m_scaleX;					// Pixels per canvas unit
	float m_scaleY;

	// Clip area

	Rect m_clipArea = new Rect();	// In canvas units

	int m_pixelsXMin;				// Drawable limits in physical pixels
	int m_pixelsXMax;
	int m_pixelsYMin;
	int m_pixelsYMax;


	// ---------------------------------------------------------------------------------------------
	// Constructors & setup


	public TextureCanvas (int pixelsWd, int pixelsHt, Rect canvasRect)
		{
		SetupCanvas(pixelsWd, pixelsHt);
		rect = canvasRect;
		}

	public TextureCanvas (int pixelsWd, int pixelsHt, float canvasWd, float canvasHt)
		{
		SetupCanvas(pixelsWd, pixelsHt);
		rect = new Rect(0.0f, 0.0f, canvasWd, canvasHt);
		}

	public TextureCanvas (int pixelsWd, int pixelsHt)
		{
		SetupCanvas(pixelsWd, pixelsHt);
		rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		}


	// Destructor - manually release the texture.
	// Use before discarding a temporary TextureCanvas object.
	// NOTE: Textures are not automatically released via GC!

	public void DestroyTexture ()
		{
		Object.DestroyImmediate(m_texture);
		}


	// Logical dimensions

	public Rect rect
		{
		get	{
			return m_canvasRect;
			}

		set	{
			m_canvasRect = value;
			m_scaleX = m_pixelsWd / m_canvasRect.width;
			m_scaleY = m_pixelsHt / m_canvasRect.height;
			clipArea = m_canvasRect;
			}
		}


	// Drawable area
	// Set to "rect" for resetting it

	public Rect clipArea
		{
		get {
			return m_clipArea;
			}

		set	{
			// It's clipped against the logical rect. If the clipped area falls outside the
			// rect the width/height will become negative (= full clip).

			m_clipArea.xMin = Mathf.Max(value.xMin, m_canvasRect.xMin);
			m_clipArea.xMax = Mathf.Min(value.xMax, m_canvasRect.xMax);
			m_clipArea.yMin = Mathf.Max(value.yMin, m_canvasRect.yMin);
			m_clipArea.yMax = Mathf.Min(value.yMax, m_canvasRect.yMax);

			m_pixelsXMin = GetPixelX(m_clipArea.xMin);
			m_pixelsXMax = GetPixelX(m_clipArea.xMax);
			m_pixelsYMin = GetPixelY(m_clipArea.yMin);
			m_pixelsYMax = GetPixelY(m_clipArea.yMax);
			}
		}


	// Unit conversions

	public float Pixels2CanvasX (int pixels) { return (float)pixels / m_scaleX; }
	public float Pixels2CanvasY (int pixels) { return (float)pixels / m_scaleY; }

 	int GetPixelX (float x) 			{ return Mathf.RoundToInt((x - m_canvasRect.xMin) * m_scaleX); }
	int GetPixelY (float y) 			{ return Mathf.RoundToInt((y - m_canvasRect.yMin) * m_scaleY); }
	int GetPixelWidth (float width) 	{ return Mathf.RoundToInt(width * m_scaleX); }
	int GetPixelHeight (float height) 	{ return Mathf.RoundToInt(height * m_scaleY); }


	// ---------------------------------------------------------------------------------------------
    // Drawing


	// When alpha is -1 (default) the alpha channel of the color property is used

	Color32 m_color = Color.white;
	float m_alpha = -1.0f;
	float m_srcAlpha = 1.0f;
	float m_dstAlpha = 0.0f;


	public Color32 color
		{
		get	{
			return m_color;
			}

		set	{
			m_color = value;
			SetupAlpha();
			}
		}


	public float alpha
		{
		get	{
			return m_alpha;
			}

		set	{
			m_alpha = value;
			SetupAlpha();
			}
		}


	public bool alphaBlend { get; set; }

	public enum LineType { Solid, Dotted, Dashed }
	public LineType lineType { get; set; }

	public int dotInterval { get; set; }
	public int dashInterval { get; set; }


	// Line draw methods


	float m_moveX;
	float m_moveY;
	int m_step;


	public void MoveTo (float x0, float y0)
		{
		m_moveX = x0;
		m_moveY = y0;
		m_step = 0;
		}


	public void LineTo (float x1, float y1)
		{
		float x0 = m_moveX;
		float y0 = m_moveY;

		m_moveX = x1;
		m_moveY = y1;

		// Ensure that x0 <= x1 for better crop calculation

		if (x0 > x1)
			{
			float swap = x0; x0 = x1; x1 = swap;
			swap = y0; y0 = y1; y1 = swap;
			}

		// Left / right crop

		float sl = (y1 - y0) / (x1 - x0);

		if (x0 < m_clipArea.xMin) { y0 += (m_clipArea.xMin - x0) * sl; x0 = m_clipArea.xMin; }
		if (x1 > m_clipArea.xMax) { y1 += (m_clipArea.xMax - x1) * sl; x1 = m_clipArea.xMax; }

		// We can now discard the lines that won't cross the visible view.

		if (x0 > m_clipArea.xMax || x1 < m_clipArea.xMin ||
			(y0 < m_clipArea.yMin && y1 < m_clipArea.yMin) || (y0 > m_clipArea.yMax && y1 > m_clipArea.yMax))
			return;

		// At this point the line necessarily crosses the visible viewport. X coords are already cropped.
		// We now crop the Y coords that may be outside the view.

		if (y0 < m_clipArea.yMin) { x0 += (m_clipArea.yMin - y0) / sl; y0 = m_clipArea.yMin; }
		if (y0 > m_clipArea.yMax) { x0 += (m_clipArea.yMax - y0) / sl; y0 = m_clipArea.yMax; }

		if (y1 < m_clipArea.yMin) { x1 += (m_clipArea.yMin - y1) / sl; y1 = m_clipArea.yMin; }
		if (y1 > m_clipArea.yMax) { x1 += (m_clipArea.yMax - y1) / sl; y1 = m_clipArea.yMax; }

		// Draw the resulting line

		TexLine(GetPixelX(x0), GetPixelY(y0), GetPixelX(x1), GetPixelY(y1));
		m_dirty = true;
		}


	public void Line (float x0, float y0, float x1, float y1)
		{
		MoveTo(x0, y0);
		LineTo(x1, y1);
		}


	public void HorizontalLine (float y)
		{
		m_step = 0;
		TexSegmentH(m_pixelsXMin, m_pixelsXMax, GetPixelY(y));
		m_dirty = true;
		}


	public void VerticalLine (float x)
		{
		m_step = 0;
		TexSegmentV(GetPixelX(x), m_pixelsYMin, m_pixelsYMax);
		m_dirty = true;
		}


	// Circle and ellipse drawing


	public void Circumference (float x, float y, float radius)
		{
		m_step = 0;
		int r = GetPixelWidth(radius);
		TexEllipse(GetPixelX(x), GetPixelY(y), r, r);
		m_dirty = true;
		}


	public void Circle (float x, float y, float radius)
		{
		m_step = 0;
		int r = GetPixelWidth(radius);
		TexFillEllipse(GetPixelX(x), GetPixelY(y), r, r);
		m_dirty = true;
		}


	public void Ellipse (float x, float y, float rx, float ry)
		{
		m_step = 0;
		TexEllipse(GetPixelX(x), GetPixelY(y), GetPixelWidth(rx), GetPixelHeight(ry));
		m_dirty = true;
		}


	public void FillEllipse (float x, float y, float rx, float ry)
		{
		m_step = 0;
		TexFillEllipse(GetPixelX(x), GetPixelY(y), GetPixelWidth(rx), GetPixelHeight(ry));
		m_dirty = true;
		}


	// Utility drawing methods


	public void Clear ()
		{
		for (int i=0, c=m_pixels.Length; i<c; i++)
			m_pixels[i] = m_color;

		m_dirty = true;
		}


	public void Grid (float stepX, float stepY)
		{
		float f;

		if (stepX < Pixels2CanvasX(2)) stepX = Pixels2CanvasX(2);
		if (stepY < Pixels2CanvasY(2)) stepY = Pixels2CanvasY(2);

		float x0 = (int)(m_canvasRect.x / stepX) * stepX;
		float y0 = (int)(m_canvasRect.y / stepY) * stepY;

		for (f=x0; f<=m_canvasRect.xMax; f+=stepX) VerticalLine(f);
		for (f=y0; f<=m_canvasRect.yMax; f+=stepY) HorizontalLine(f);
		}


	public void Dot (float x, float y)
		{
		int px = GetPixelX(x);
		int py = GetPixelY(y);

		TexPixel(px, py-1);
		TexPixel(px-1, py);
		TexPixel(px, py);
		TexPixel(px+1, py);
		TexPixel(px, py+1);
		m_dirty = true;
		}


	public void Cross (float x, float y, int radiusX, int radiusY)
		{
		int px = GetPixelX(x);
		int py = GetPixelY(y);

		for (int i = px-radiusX; i <= px+radiusX; i++)
			TexPixel(i, py);

		for (int j = py-radiusY; j <= py+radiusY; j++)
			TexPixel(px, j);

		m_dirty = true;
		}


	public void FillRect (float x, float y, float width, float height)
		{
		int x0 = GetPixelX(x);
		int y0 = GetPixelY(y);
		int x1 = GetPixelX(x + width);
		int y1 = GetPixelY(y + height);

		if (y1 < y0)
			{
			int swap = y0;
			y0 = y1;
			y1 = swap;
			}

        for (int i = y0; i <= y1; i++)
			{
			m_step = 0;
			TexSegmentH(x0, x1, i);
			}

		m_dirty = true;
		}


	// Function plot


	public int functionResolution { get; set; }


	public void Function (System.Func<float, float> func, float x0, float x1)
		{
		float stepSize = Pixels2CanvasX(functionResolution);

		MoveTo(x0, func(x0));

		float x;
		for (x = x0; x <= x1; x += stepSize)
			LineTo(x, func(x));

		if (x < x1)
			LineTo(x1, func(x1));
		}


	public void Function (System.Func<float, float> func)
		{
		Function(func, m_canvasRect.xMin, m_canvasRect.xMax);
		}


	public void SolidFunction (System.Func<float, float> func, float x0, float x1)
		{
		int px0 = GetPixelX(x0);
		int px1 = GetPixelX(x1);
		int pxWidth = px1 - px0;

		int py0 = GetPixelY(0);
		int py1;

		for (int px = 0; px <= pxWidth; px++)
			{
			m_step = 0;

			py1 = GetPixelHeight(func(x0 + Pixels2CanvasX(px)));
			TexSegmentV(px0+px, py0, py0+py1);
			}

		m_dirty = true;
		}


	public void SolidFunction (System.Func<float, float> func)
		{
		SolidFunction(func, m_canvasRect.xMin, m_canvasRect.xMax);
		}


	// ---------------------------------------------------------------------------------------------
	// Tools & GUI drawing


	// Save current canvas & restore.
	// Pixels only! No logical rect / clip area.


	public void Save ()
		{
		if (m_buffer == null)
			m_buffer = (m_pixels.Clone() as Color32[]);
		else
			m_pixels.CopyTo(m_buffer, 0);
		}


	public void Restore ()
		{
		if (m_buffer != null)
			{
			m_buffer.CopyTo(m_pixels, 0);
			m_dirty = true;
			}
		}


	// GUI draw


	public void GUIDraw (int x, int y)
		{
        ApplyChanges();
		GUI.DrawTexture(new Rect(x, y, m_pixelsWd, m_pixelsHt), m_texture);
		}


	#if UNITY_EDITOR
	public void EditorGUIDraw (Rect position)
		{
        ApplyChanges();
		UnityEditor.EditorGUI.DropShadowLabel(position, new GUIContent(m_texture));
		}
	#endif


	public void GUIStretchDraw (int x, int y, int width, int height)
		{
		ApplyChanges();
		GUI.DrawTexture(new Rect(x, y, width, height), m_texture);
		}


	public void GUIStretchDraw (int x, int y, int width)
		{
		ApplyChanges();
		float ratio = (float)m_pixelsHt / m_pixelsWd;
		GUI.DrawTexture(new Rect(x, y, width, width * ratio), m_texture);
		}


	// Access to the actual Texture2D object for custom usage


	public Texture2D texture
		{
		get {
			ApplyChanges();
			return m_texture;
			}
		}


	// ---------------------------------------------------------------------------------------------
	// Private / Internal


    void ApplyChanges ()
		{
		if (m_dirty)
			{
			m_texture.SetPixels32(m_pixels);
			m_texture.Apply(false);
			m_dirty = false;
			}
		}


	void SetupCanvas (int pixelsWd, int pixelsHt)
		{
		m_texture = new Texture2D(pixelsWd, pixelsHt, TextureFormat.ARGB32, false, true);
		m_texture.hideFlags = HideFlags.HideAndDontSave;

		// Pixels are stored in both int and float formats for optimizing each type of operation

		m_pixelsWd = pixelsWd;
		m_pixelsHt = pixelsHt;
		m_pixels = new Color32[pixelsWd * pixelsHt];

		// Default value for auto-properties and fields

		alphaBlend = false;
		dotInterval = 5;
		dashInterval = 5;
		functionResolution = 3;
		}


	void SetupAlpha ()
		{
		if (m_alpha >= 0.0f)
			m_color.a = (byte)(Mathf.Clamp01(m_alpha) * 255.0f);

		m_srcAlpha = m_color.a / 255.0f;
		m_dstAlpha = 1.0f - m_srcAlpha;
		}


	Color32 GetAlphaBlendedPixel (Color32 dst)
		{
		return new Color32(
			(byte)(m_color.r*m_srcAlpha + dst.r*m_dstAlpha),
			(byte)(m_color.g*m_srcAlpha + dst.g*m_dstAlpha),
			(byte)(m_color.b*m_srcAlpha + dst.b*m_dstAlpha),
			(byte)(m_color.a*m_srcAlpha + dst.a*m_dstAlpha));
		}


	// ---------------------------------------------------------------------------------------------
	// Private low-level drawing functions


	bool CheckForPixel ()
		{
		if (lineType == LineType.Solid)
			return true;

		if (lineType == LineType.Dotted)
			return (m_step++ % dotInterval) == 0;

		if (lineType == LineType.Dashed)
			{
			int n = dashInterval;
            return (m_step++ % (n * 2)) < n;
			}

		return true;
		}


	// PutPixel


	void TexPixel (int x, int y)
		{
		if (x >= m_pixelsXMin && x < m_pixelsXMax && y >= m_pixelsYMin && y < m_pixelsYMax)
			{
			int pixel = y * m_pixelsWd + x;
			m_pixels[pixel] = alphaBlend? GetAlphaBlendedPixel(m_pixels[pixel]) : m_color;
			}
		}


	// Line drawing


	void TexLine (int x0, int y0, int x1, int y1)
		{
		int dy = y1 - y0;
		int dx = x1 - x0;

		if (dx == 0)
			TexSegmentV(x0, y0, y1);
		else
		if (dy == 0)
			TexSegmentH(x0, x1, y0);
		else
			{
			int stepY;
			if (dy < 0)
				{
				dy = -dy;
				stepY = -1;
				}
			else
				{
				stepY = 1;
				}

			int stepX;
			if (dx < 0)
				{
				dx = -dx;
				stepX = -1;
				}
			else
				{
				stepX = 1;
				}

			dy <<= 1;
			dx <<= 1;

			if (CheckForPixel())
				TexPixel(x0, y0);

			if (dx > dy)
				{
				int fraction = dy - (dx >> 1);
				while (x0 != x1)
					{
					if (fraction >= 0)
						{
						y0 += stepY;
						fraction -= dx;
						}
					x0 += stepX;
					fraction += dy;

					if (CheckForPixel())
						TexPixel(x0, y0);
					}
				}
			else
				{
				int fraction = dx - (dy >> 1);
				while (y0 != y1)
					{
					if (fraction >= 0)
						{
						x0 += stepX;
						fraction -= dy;
						}
					y0 += stepY;
					fraction += dx;

					if (CheckForPixel())
						TexPixel(x0, y0);
					}
				}
			}
		}


	// Fast segment drawing functions.
	// They avoid the CheckForPixel check when possible.


	void TexSegmentV (int x, int y0, int y1)
		{
		if (y0 > y1)
			{
			int swap = y0;
			y0 = y1;
			y1 = swap;
			}

		if (x < m_pixelsXMin || x >= m_pixelsXMax || y1 < m_pixelsYMin || y0 >= m_pixelsYMax) return;

		if (y0 < m_pixelsYMin) y0 = m_pixelsYMin;
		if (y1 >= m_pixelsYMax) y1 = m_pixelsYMax;

		int pixel = y0 * m_pixelsWd + x;

		if (!alphaBlend)
			{
			if (lineType == LineType.Solid)
				{
				for (int y = y0; y < y1; y++)
					{
					m_pixels[pixel] = m_color;
					pixel += m_pixelsWd;
					}
				}
			else
				{
				for (int y = y0; y < y1; y++)
					{
					if (CheckForPixel())
						m_pixels[pixel] = m_color;
					pixel += m_pixelsWd;
					}
				}
			}
		else
			{
			if (lineType == LineType.Solid)
				{
				for (int y = y0; y < y1; y++)
					{
					m_pixels[pixel] = GetAlphaBlendedPixel(m_pixels[pixel]);
					pixel += m_pixelsWd;
					}
				}
			else
				{
				for (int y = y0; y < y1; y++)
					{
					if (CheckForPixel())
						m_pixels[pixel] = GetAlphaBlendedPixel(m_pixels[pixel]);
					pixel += m_pixelsWd;
					}
				}
			}
		}


	void TexSegmentH (int x0, int x1, int y)
		{
		if (x0 > x1)
			{
			int swap = x0;
			x0 = x1;
			x1 = swap;
			}

		if (y < m_pixelsYMin || y >= m_pixelsYMax || x1 < m_pixelsXMin || x0 >= m_pixelsXMax) return;

		if (x0 < m_pixelsXMin) x0 = m_pixelsXMin;
		if (x1 > m_pixelsXMax) x1 = m_pixelsXMax;

		int pixel = y * m_pixelsWd + x0;

		if (!alphaBlend)
			{
			if (lineType == LineType.Solid)
				{
				for (int x = x0; x < x1; x++)
					m_pixels[pixel++] = m_color;
				}
			else
				{
				for (int x = x0; x < x1; x++)
					{
					if (CheckForPixel())
						m_pixels[pixel] = m_color;
					pixel++;
					}
				}
			}
		else
			{
			if (lineType == LineType.Solid)
				{
				for (int x = x0; x < x1; x++)
					{
					m_pixels[pixel] = GetAlphaBlendedPixel(m_pixels[pixel]);
					pixel++;
					}
				}
			else
				{
				for (int x = x0; x < x1; x++)
					{
					if (CheckForPixel())
						m_pixels[pixel] = GetAlphaBlendedPixel(m_pixels[pixel]);
					pixel++;
					}
				}
			}
		}


	// Ellipse / circle drawing


	void TexEllipse (int cx, int cy, int rx, int ry)
		{
		if (rx >= ry)
			{
			int y = rx;
			int d = -rx;
			int end = (int)Mathf.Ceil(rx / Mathf.Sqrt(2.0f));

			float sy = (float)ry / rx;

			for (int x = 0; x <= end; x++)
				{
				TexPixel(cx+x, (int)(cy+y*sy));
				TexPixel(cx+x, (int)(cy-y*sy));
				TexPixel(cx-x, (int)(cy+y*sy));
				TexPixel(cx-x, (int)(cy-y*sy));

				TexPixel(cx+y, (int)(cy+x*sy));
				TexPixel(cx-y, (int)(cy+x*sy));
				TexPixel(cx+y, (int)(cy-x*sy));
				TexPixel(cx-y, (int)(cy-x*sy));

				d += 2*x + 1;
				if (d > 0) d += 2 - 2*y--;
				}
			}
		else
			{
			int x = ry;
			int d = -ry;
			int end = (int)Mathf.Ceil(ry / Mathf.Sqrt(2.0f));

			float sx = (float)rx / ry;

			for (int y = 0; y <= end; y++)
				{
				TexPixel((int)(cx+y*sx), cy+x);
				TexPixel((int)(cx+y*sx), cy-x);
				TexPixel((int)(cx-y*sx), cy+x);
				TexPixel((int)(cx-y*sx), cy-x);

				TexPixel((int)(cx+x*sx), cy+y);
				TexPixel((int)(cx-x*sx), cy+y);
				TexPixel((int)(cx+x*sx), cy-y);
				TexPixel((int)(cx-x*sx), cy-y);

				d += 2*y + 1;
				if (d > 0) d += 2 - 2*x--;
				}
			}
		}


	void TexFillEllipse (int cx, int cy, int rx, int ry)
		{
		if (rx >= ry)
			{
			int y = rx;
			int d = -rx;
			int end = (int)Mathf.Ceil(rx / Mathf.Sqrt(2.0f));

			float sy = (float)ry / rx;

			for (int x = 0; x <= end; x++)
				{
				TexSegmentV(cx+x, cy, (int)(cy+y*sy));
				TexSegmentV(cx+x, cy, (int)(cy-y*sy));
				TexSegmentV(cx-x, cy, (int)(cy+y*sy));
				TexSegmentV(cx-x, cy, (int)(cy-y*sy));

				TexSegmentV(cx+y, cy, (int)(cy+x*sy));
				TexSegmentV(cx-y, cy, (int)(cy+x*sy));
				TexSegmentV(cx+y, cy, (int)(cy-x*sy));
				TexSegmentV(cx-y, cy, (int)(cy-x*sy));

				d += 2*x + 1;
				if (d > 0) d += 2 - 2*y--;
				}
			}
		else
			{
			int x = ry;
			int d = -ry;
			int end = (int)Mathf.Ceil(ry / Mathf.Sqrt(2.0f));

			float sx = (float)rx / ry;

			for (int y = 0; y <= end; y++)
				{
				TexSegmentH((int)(cx+y*sx), cx, cy+x);
				TexSegmentH((int)(cx+y*sx), cx, cy-x);
				TexSegmentH((int)(cx-y*sx), cx, cy+x);
				TexSegmentH((int)(cx-y*sx), cx, cy-x);

				TexSegmentH((int)(cx+x*sx), cx, cy+y);
				TexSegmentH((int)(cx-x*sx), cx, cy+y);
				TexSegmentH((int)(cx+x*sx), cx, cy-y);
				TexSegmentH((int)(cx-x*sx), cx, cy-y);

				d += 2*y + 1;
				if (d > 0) d += 2 - 2*x--;
				}
			}
		}
	}
}