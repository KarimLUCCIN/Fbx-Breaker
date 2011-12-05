using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX;

namespace FbxBreak.XFileWriter
{
    public class TypeConverter
    {
        #region Type Convertor(used by XML Serialization)
        public static string Vector3ToString(Vector3 v)
        {
            return v.X + "," + v.Y + "," + v.Z;
        }
        public static Vector3 StringToVector3(string vStr)
        {
            if (string.IsNullOrEmpty(vStr))
                return new Vector3();
            string[] vStrs = vStr.Split(',');
            return new Vector3(float.Parse(vStrs[0]), float.Parse(vStrs[1]), float.Parse(vStrs[2]));
        }
        public static string Vector4ToString(Vector4 v)
        {
            return v.X + "," + v.Y + "," + v.Z + "," + v.W;
        }
        public static Vector4 StringToVector4(string vStr)
        {
            if (string.IsNullOrEmpty(vStr))
                return new Vector4();
            string[] vStrs = vStr.Split(',');
            return new Vector4(float.Parse(vStrs[0]), float.Parse(vStrs[1]), float.Parse(vStrs[2]), float.Parse(vStrs[3]));
        }
        public static string MatrixToString(Matrix m)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m.M11 + "," + m.M12 + "," + m.M13 + "," + m.M14 + ",");
            sb.Append(m.M21 + "," + m.M22 + "," + m.M23 + "," + m.M24 + ",");
            sb.Append(m.M31 + "," + m.M32 + "," + m.M33 + "," + m.M34 + ",");
            sb.Append(m.M41 + "," + m.M42 + "," + m.M43 + "," + m.M44);
            return sb.ToString();

        }
        public static Matrix StringToMatrix(string mStr)
        {
            Matrix m;
            string[] strs = mStr.Split(',');
            if (strs.Length == 0)
                return Matrix.Identity;
            m.M11 = float.Parse(strs[0]);
            m.M12 = float.Parse(strs[1]);
            m.M13 = float.Parse(strs[2]);
            m.M14 = float.Parse(strs[3]);
            m.M21 = float.Parse(strs[4]);
            m.M22 = float.Parse(strs[5]);
            m.M23 = float.Parse(strs[6]);
            m.M24 = float.Parse(strs[7]);
            m.M31 = float.Parse(strs[8]);
            m.M32 = float.Parse(strs[9]);
            m.M33 = float.Parse(strs[10]);
            m.M34 = float.Parse(strs[11]);
            m.M41 = float.Parse(strs[12]);
            m.M42 = float.Parse(strs[13]);
            m.M43 = float.Parse(strs[14]);
            m.M44 = float.Parse(strs[15]);
            return m;
        }

        public static Vector3[] StringToPoints(string mStr)
        {
            string[] strs = mStr.TrimEnd().Split(' ');
            Vector3[] m = new Vector3[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                string[] xyz = strs[i].Split(',');
                m[i] = new Vector3(Convert.ToSingle(xyz[0]), Convert.ToSingle(xyz[1]), Convert.ToSingle(xyz[2]));
            }
            return m;
        }

        public static string PointsToString(Vector3[] Points)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Points.Length; i++)
            {
                sb.Append(Points[i].X + "," + Points[i].Y + "," + Points[i].Z + " ");
            }
            return sb.ToString();
        }
        #endregion
    }
}
