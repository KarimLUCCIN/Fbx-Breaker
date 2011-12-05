using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Microsoft.DirectX;

namespace FbxBreak.XFileWriter
{
    [Serializable]
    public class XmlMaterialList
    {
        [XmlArray("Materials")]
        public List<XmlMaterial> materialList = new List<XmlMaterial>();
        public static bool Save(XmlMaterialList xml, string url)
        {
            using (Stream stream = File.Open(url, FileMode.Create))
            {
                XmlSerializer bformatter = new XmlSerializer(typeof(XmlMaterialList));
                bformatter.Serialize(stream, xml);
                stream.Flush();
            }
            return true;
        }
        public static XmlMaterialList Load(string url)
        {
            XmlMaterialList mat = null; ;
            //TODO:
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlMaterialList));
            using (Stream stream = File.Open(url, FileMode.Open))
            {
                mat = (XmlMaterialList)xmlSerializer.Deserialize(stream);
            }
            return mat;
        }
    }

    [Serializable]
    public class XmlMaterial
    {
        //<3d82ab4d-62da-11cf-ab39-0020af71e433>
        [XmlIgnore]
        public Vector4 faceColor;
        [XmlIgnore]
        public float power;
        [XmlIgnore]
        public Vector3 specularColor;
        [XmlIgnore]
        public Vector3 emissiveColor;
        [XmlAttribute("FrameMeshName")]
        public string frameMeshName;
        [XmlAttribute("TextureFileName")]
        public string textureFileName;

        [XmlAttribute("FaceColor")]
        public string FaceColor
        {
            get { return TypeConverter.Vector4ToString(faceColor); }
            set { faceColor = TypeConverter.StringToVector4(value); }
        }
        [XmlAttribute("Power")]
        public float Power
        {
            get { return power; }
            set { power = value; }
        }
        [XmlAttribute("SpecularColor")]
        public string SpecularColor
        {
            get { return TypeConverter.Vector3ToString(specularColor); }
            set { specularColor = TypeConverter.StringToVector3(value); }
        }
        [XmlAttribute("EmissiveColor")]
        public string EmissiveColor
        {
            get { return TypeConverter.Vector3ToString(emissiveColor); }
            set { emissiveColor = TypeConverter.StringToVector3(value); }
        }
    }
}
