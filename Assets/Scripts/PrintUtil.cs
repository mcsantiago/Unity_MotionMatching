using System;
using System.Text;

public static class PrintUtil
{
  public static string FormatFloatToString(float[] a)
  {
    StringBuilder sb = new StringBuilder();

    foreach (float f in a)
    {
      sb.Append(string.Format("{0:F1}", f));
      sb.Append(", ");
    }
    sb.Remove(sb.Length - 2, 2);

    return sb.ToString();
  }
}