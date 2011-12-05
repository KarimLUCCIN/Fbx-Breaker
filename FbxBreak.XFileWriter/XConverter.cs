using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace FbxBreak.XFileWriter
{
    public class Dummy
    {

    }
    public class XSkinInfo : IDisposable
    {
        class XBoneInfo
        {
            public List<int> Indices = new List<int>();
            public List<float> Weights = new List<float>();
            public Matrix BoneMatrix = Matrix.Identity;
        }
        Dictionary<string, XBoneInfo> _boneInfo = new Dictionary<string, XBoneInfo>();
        public void AddVertex(string boneName, int vexindex, float vexweight)
        {
            XBoneInfo bone = null;
            if (_boneInfo.ContainsKey(boneName))
            {
                bone = _boneInfo[boneName];
            }
            else
            {
                bone = new XBoneInfo();
                _boneInfo.Add(boneName, bone);
            }
            bone.Indices.Add(vexindex);
            bone.Weights.Add(vexweight);
        }
        public Dictionary<string, XStream> GetMeshSkinInfoBytes()
        {
            Dictionary<string, XStream> meshskin = new Dictionary<string, XStream>();
            foreach (string bonename in _boneInfo.Keys)
            {
                XBoneInfo bi = _boneInfo[bonename];
                XStream xs = new XStream();
                xs.Append(XStream.ValidString(bonename));
                int icount = bi.Indices.Count;
                xs.Append<int>(icount);
                for (int i = 0; i < icount; i++)
                {
                    xs.Append<int>(bi.Indices[i]);
                }
                for (int i = 0; i < icount; i++)
                {
                    xs.Append<float>(bi.Weights[i]);
                }
                xs.Append<Matrix>(bi.BoneMatrix);
                //
                meshskin.Add(bonename, xs);
            }
            return meshskin;
        }

        internal void SetBoneMatrix(FbxBoneInfo fbx)
        {
            foreach (string key in _boneInfo.Keys)
            {
                if (fbx.LinkMatrixes.ContainsKey(key))
                {
                    XBoneInfo xb = _boneInfo[key];
                    // xs.Append<Matrix>(fbx.LinkMatrixes[key]);
                    xb.BoneMatrix = fbx.LinkMatrixes[key];
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _boneInfo.Clear();
            _boneInfo = null;
        }

        #endregion
    }
    public class XStream : IDisposable
    {
        MemoryStream _mem = null;
        BinaryWriter _writer = null;
        public XStream()
        {
            _mem = new MemoryStream();
            _writer = new BinaryWriter(_mem);
        }
        public void Append<T>(T t)
        {
            WriteToStream<T>(t, _writer);
        }
        public void Append(string t)
        {
            char[] temp = t.ToCharArray();
            _writer.Write(temp);
            _writer.Write('\0');
        }
        public void Append<T>(T[] t)
        {
            for (int i = 0; i < t.Length; i++)
            {
                WriteToStream<T>(t[i], _writer);
            }
        }
        public byte[] ToArray()
        {
            _writer.Flush();
            return _mem.ToArray();
        }
        public long Length
        {
            get
            {
                _writer.Flush();
                return _mem.Length;
            }
        }
        //public ~XStream()
        //{
        //    _writer.Flush();
        //    _mem.Close();
        //}
        public static byte[] ToBytes<T>(T t)
        {
            byte[] temp = new byte[Marshal.SizeOf(t)];
            //IntPtr sourcePtr = Marshal.UnsafeAddrOfPinnedArrayElement(temp, 0);
            //Marshal.StructureToPtr(t, sourcePtr, true);

            int vLen = Marshal.SizeOf(t);
            IntPtr pnt = Marshal.AllocHGlobal(vLen);
            try
            {
                // Copy the struct to unmanaged memory.
                Marshal.StructureToPtr(t, pnt, false);
                Marshal.Copy(pnt, temp, 0, vLen);
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pnt);
            }


            return temp;
        }
        public static byte[] ToBytes(string t)
        {
            //byte[] temp = Encoding.Default.GetBytes(t);
            //Encoding.ASCII
            //char[] temp = t.ToCharArray();
            //_writer.Write(temp);
            //_writer.Write('\0');

            //byte[] temp = Encoding.Default.GetBytes(t);

            int len = t.Length + 1;
            char[] t1 = new char[len];
            t.CopyTo(0, t1, 0, len - 1);
            byte[] temp = new byte[len];
            for (int i = 0; i < len - 1; i++)
            {
                temp[i] = (byte)t1[i];
            }
            temp[len - 1] = (byte)0;
            return temp;
        }
        public static void WriteToStream<T>(T t, BinaryWriter writer)
        {
            byte[] temp = ToBytes<T>(t);
            writer.Write(temp, 0, temp.Length);
        }
        public static string ValidString(string name)
        {
            string vv = name.Replace(' ', '_');
            vv = vv.Replace('.', '_');
            vv = vv.Replace(':', '_');
            return vv;
        }
        static byte[] zero = null;
        public static byte[] ZeroBytes
        {
            get
            {
                if (zero == null)
                {
                    zero = new byte[1];
                }
                return zero;
            }
        }

        #region IDisposable ³ÉÔ±

        void IDisposable.Dispose()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
            }
            if (_mem != null)
            {
                _mem.Close();
            }
        }

        #endregion
    }
    public class XConverter
    {
        static string XSKINEXP_TEMPLATES = @"xof 0303txt 0032 
                                template XSkinMeshHeader  
                                {  
                                    <3CF169CE-FF7C-44ab-93C0-F78F62D172E2>  
                                    WORD nMaxSkinWeightsPerVertex;  
                                    WORD nMaxSkinWeightsPerFace;  
                                    WORD nBones;  
                                }  
                                template VertexDuplicationIndices  
                                {  
                                    <B8D65549-D7C9-4995-89CF-53A9A8B031E3>  
                                    DWORD nIndices;  
                                    DWORD nOriginalVertices;  
                                    array DWORD indices[nIndices];  
                                }  
                                template SkinWeights  
                                {  
                                    <6F0D123B-BAD2-4167-A0D0-80224F25FABB>  
                                    STRING transformNodeName; 
                                    DWORD nWeights;  
                                    array DWORD vertexIndices[nWeights];  
                                    array float weights[nWeights];  
                                    Matrix4x4 matrixOffset;  
                                }
                                template FVFData
                                {
                                    <B6E70A0E-8EF9-4e83-94AD-ECC8B0C04897>
                                    DWORD dwFVF; 
                                    DWORD nDWords;
                                    array DWORD data[nDWords];
                                }";
        static Guid SkinHeaderGuid = new Guid(@"3CF169CE-FF7C-44ab-93C0-F78F62D172E2");
        static Guid SkinWeightGuid = new Guid(@"6F0D123B-BAD2-4167-A0D0-80224F25FABB");
        static Guid FVFDataGuid = new Guid(@"B6E70A0E-8EF9-4e83-94AD-ECC8B0C04897");
        static int FPS = 4800;
        Dictionary<string, string> _cacheToCopy = new Dictionary<string, string>();
        List<string> _usedTextures = new List<string>();
        List<string> _notExists = new List<string>();
        XmlMaterialList _usedMaterials = new XmlMaterialList();
        StringBuilder _modelConfigItems = new StringBuilder();
        string _sourceDir = "";
        bool _hasAnimation = true;
        bool _withMesh = true;

        #region Console Window property stuff
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        #endregion //Console
        private static IntPtr ThisConsole = GetConsoleWindow();

        static Device _device = null;


        private bool optDeleteMeshesWithoutSkinBeforeExporting = false;

        public bool OptDeleteMeshesWithoutSkinBeforeExporting
        {
            get { return optDeleteMeshesWithoutSkinBeforeExporting; }
            set { optDeleteMeshesWithoutSkinBeforeExporting = value; }
        }

        private bool optDoNotOptimize = false;

        public bool OptDoNotOptimize
        {
            get { return optDoNotOptimize; }
            set { optDoNotOptimize = value; }
        }

        public static Device Device
        {
            get
            {
                if (_device == null)
                {
                    AdapterInformation ai = Manager.Adapters.Default;

                    PresentParameters pp = new PresentParameters();
                    pp.BackBufferWidth = 1;
                    pp.BackBufferHeight = 1;
                    pp.BackBufferFormat = ai.CurrentDisplayMode.Format;
                    pp.BackBufferCount = 1;
                    pp.SwapEffect = SwapEffect.Copy;
                    pp.Windowed = true;

                    _device = new Device(0, DeviceType.NullReference, ThisConsole,
                                        CreateFlags.HardwareVertexProcessing, pp);

                }
                return _device;
            }
            set { _device = value; }
        }



        private void Init()
        {
            _cacheToCopy.Clear();
            _usedTextures.Clear();
            _notExists.Clear();
            _usedMaterials.materialList.Clear();
            _modelConfigItems = new StringBuilder();
            _sourceDir = "";
            _hasAnimation = true;
        }


        public void ConvertFbx2X(string sourceDir, string outputX, FbxSkeletalNode root, FbxSkeletalNode model, FbxAnimationCollection col, bool separateAnimationFromMesh, bool copyTextures)
        {
            string outfilename = Path.GetFileNameWithoutExtension(outputX);

            string outurl = outputX;
            string outanimurl = outfilename + "_anim.x";
            string outputdir = Path.GetDirectoryName(outurl);
            if (!Directory.Exists(outputdir))
            {
                Directory.CreateDirectory(outputdir);
            }

            this.Init();
            //
            _sourceDir = sourceDir;
            //
            List<string> cacheMissedBone = new List<string>();

#warning TO MIMIC
            //FbxParser parser = new FbxParser(fbx, CommandParser.setting.bZuP, CommandParser.setting.dUnitScale);
            //// parser.SetDebugMode(true);
            //parser.SetSwapWindingOrder(CommandParser.setting.bSwapWindingOrder);
            //parser.SetCoordinate(CommandParser.setting.bLeftHand);
            //FbxSkeletalNode root = parser.GetSkeletalHierarchy();
            //FbxSkeletalNode model = parser.GetModel();
            //FbxAnimationCollection col = parser.GetAnimation();

            if (col == null || col.Children.Count == 0) _hasAnimation = false;

            /* text format doesn't work ... */
            XFileFormat outType = XFileFormat.Binary;
            if (!separateAnimationFromMesh)
            {
                //outType = XFileFormat.Text;
                if (!outurl.ToLower().EndsWith(".x"))
                {
                    outurl = outurl + ".x";
                }
                using (XFileManager xfm = new XFileManager())
                {
                    File.Delete(outurl);
                    XFileSaveObject so = xfm.CreateSaveObject(outurl, outType);
                    
                    xfm.RegisterDefaultTemplates();
                    //skin
                    xfm.RegisterTemplates(Encoding.Default.GetBytes(XSKINEXP_TEMPLATES));
                    //default withmesh
                    LoadFromFbx(model, so);
                    //animation
                    ConvertAnimation(col, so);
                    so.Save();
                    so.Dispose();
                }
            }
            else
            {
                using (XFileManager xfm = new XFileManager())
                {
                    File.Delete(outurl);
                    XFileSaveObject so = xfm.CreateSaveObject(outurl, outType);
                    xfm.RegisterDefaultTemplates();
                    //skin
                    xfm.RegisterTemplates(Encoding.Default.GetBytes(XSKINEXP_TEMPLATES));
                    //default with mesh
                    LoadFromFbx(model, so);

                    so.Save();
                    so.Dispose();
                }

                //animation
                if (_hasAnimation)
                {

                    using (XFileManager xfm = new XFileManager())
                    {
                        File.Delete(outanimurl);
                        XFileSaveObject so = xfm.CreateSaveObject(outanimurl, outType);
                        xfm.RegisterDefaultTemplates();
                        //without mesh
                        _withMesh = false;
                        LoadFromFbx(model, so);
                        ConvertAnimation(col, so);

                        so.Save();
                        so.Dispose();
                    }

                }
            }

            //copy 
            if (copyTextures)
            {
                foreach (string texture in _cacheToCopy.Keys)
                {
                    string tf = Path.GetFileName(texture);
                    string dest = Path.GetFullPath(Path.Combine(outputdir, tf));
                    string src = Path.GetFullPath(texture);

                    if (!src.ToLower().Equals(dest.ToLower()))
                    {
                        File.Copy(texture, dest, true);
                    }
                }
            }
        }

        private void SaveConfigData(string configFileName)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(configFileName, FileMode.Create, FileAccess.Write)))
            {
                writer.WriteLine("<?xml version=\"1.0\"?>");
                writer.WriteLine("<ModelConfig xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" >");
                writer.WriteLine("   <Items>");
                writer.Write(_modelConfigItems.ToString());
                writer.WriteLine("   </Items>");
                writer.WriteLine("</ModelConfig>");
            }
        }

        private void LoadFromFbx(FbxSkeletalNode model, XFileSaveObject ac)
        {
            byte[] zero = new byte[1];
            Dictionary<string, Guid> dirMat = new Dictionary<string, Guid>();
            string rootname = XStream.ValidString("Scene Root");
            XFileSaveData frame = ac.AddDataObject(XFileGuid.Frame, rootname, Guid.Empty, XStream.ZeroBytes);

            ConvertNode(model, frame);
        }

        private void ConvertAnimation(FbxAnimationCollection col, XFileSaveObject root)
        {
            foreach (FbxAnimationTake take in col.Children)
            {
                //XFileSaveData xset = frame.AddDataObject(XFileGuid.AnimationSet, XStream.ValidString( take.Name), Guid.Empty, XStream.ZeroBytes);
                XFileSaveData xset = root.AddDataObject(XFileGuid.AnimationSet, "", Guid.Empty, XStream.ZeroBytes);
                foreach (FbxAnimationPart part in take.Children)
                {
                    string objname = XStream.ValidString(part.ObjectName);
                    if (string.IsNullOrEmpty(objname)) continue;
                    XFileSaveData xanim = xset.AddDataObject(XFileGuid.Animation, "", Guid.Empty, XStream.ZeroBytes);
                    XStream skeys = new XStream();
                    skeys.Append<int>(4);
                    skeys.Append<int>(part.Children.Count);

                    int firsttime = -1;
                    foreach (FbxAnimationKey key in part.Children)
                    {
                        int time = key.Time * FPS / take.FPS;
                        if (firsttime == -1)
                        {
                            firsttime = time;
                        }
                        skeys.Append<int>(time - firsttime);
                        skeys.Append<int>(16);
                        float[] temp = GetMatrixBytes(key.Matrix);
                        for (int i = 0; i < 16; i++)
                        {
                            skeys.Append<float>(temp[i]);
                        }

                    }
                    XFileSaveData xkeys = xanim.AddDataObject(XFileGuid.AnimationKey, "", Guid.Empty, skeys.ToArray());

                    xanim.AddDataReference(objname, Guid.Empty);
                }
            }
        }

        private void ConvertKey(FbxAnimationKey key, XFileSaveData xkeys, int origFPS)
        {
            XStream skey = new XStream();

            int time = key.Time * FPS / origFPS;
            skey.Append<int>(time);
            skey.Append<int>(16);
            float[] temp = GetMatrixBytes(key.Matrix);
            for (int i = 0; i < 16; i++)
            {
                skey.Append<float>(temp[i]);
            }

            xkeys.AddDataObject(XFileGuid.TimedFloatKeys, "", Guid.Empty, skey.ToArray());

        }
        private void ConvertMesh(FbxMesh mesh, XFileSaveData frame)
        {
            if (!mesh.HasGeometry) return;
            FbxMeshBuilder.calculateTriangleNormals(mesh.Indexes, mesh.Vertexes);
            //if (CommandParser.setting.bSwapWindingOrder)
            //{
            //FbxMeshBuilder.SwapWindingOrder(mesh);
            //}
            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                FbxMeshPart part = mesh.MeshParts[i];
                ConvertMeshPart(mesh, part, frame);
            }

        }
        private void ConvertMeshPart(FbxMesh mesh, FbxMeshPart part, XFileSaveData frame)
        {
            string validName = XStream.ValidString(part.Name);
            XFileSaveData f2 = frame.AddDataObject(XFileGuid.Frame, validName, Guid.Empty, XStream.ZeroBytes);
            f2.AddDataObject(XFileGuid.FrameTransformMatrix, "", Guid.Empty, XStream.ToBytes<Matrix>(Matrix.Identity));
            //
            if (!_withMesh) return;
            //

            _modelConfigItems.AppendLine(string.Format("         <ModelConfigItem ItemName=\"" + validName + "\" OptionMeshNames=\"" + validName + "\" MustHave=\"false\" />"));

            int indNumber = part.PrimitiveCount * 3;
            int vexNumber = part.NumVertices;
            int[] partIndices = new int[indNumber];
            FBXVertex[] partVextexes = new FBXVertex[vexNumber];

            for (int j = 0; j < indNumber / 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    partIndices[j * 3 + k] = mesh.Indexes[j * 3 + k + part.StartIndex] - part.BaseVertex;
                }
            }

            for (int j = 0; j < vexNumber; j++)
            {
                partVextexes[j] = mesh.Vertexes[j + part.BaseVertex];
            }

            if (!optDoNotOptimize)
            {
                // OptimizeMeshPart(
                int[] qIndices = Geometry.OptimizeFaces(partIndices, vexNumber);
                int[] tempindices = partIndices;
                partIndices = new int[indNumber];
                for (int i = 0; i < indNumber / 3; i++)
                {
                    int ni = qIndices[i];
                    partIndices[3 * i] = tempindices[3 * ni];
                    partIndices[3 * i + 1] = tempindices[3 * ni + 1];
                    partIndices[3 * i + 2] = tempindices[3 * ni + 2];
                }


                int[] qVextexes = Geometry.OptimizeVertices(partIndices, vexNumber);
                int vexCount = 0;
                for (int i = 0; i < vexNumber; i++)
                {
                    if (qVextexes[i] == -1) break;
                    vexCount++;
                }
                FBXVertex[] tempVex = new FBXVertex[vexCount];
                for (int i = 0; i < vexCount; i++)
                {
                    tempVex[qVextexes[i]] = partVextexes[i];
                }
                vexNumber = vexCount;
                partVextexes = tempVex;

                for (int i = 0; i < indNumber; i++)
                {
                    partIndices[i] = qVextexes[partIndices[i]];
                }

            }

            XStream xmesh = new XStream();
            //XStream xface = new XStream();
            XStream xnormal = new XStream();
            //List<XStream> xtextures = new List<XStream>();
            XStream xtexture0 = new XStream();
            XStream xfvf = new XStream();

            // XStream xtexture = new XStream();
            XStream xcolor = new XStream();
            bool hasColor = false;
            bool hasTexture = true;
            xmesh.Append<int>(vexNumber);
            xnormal.Append<int>(vexNumber);
            xtexture0.Append<int>(vexNumber);
            xcolor.Append<int>(vexNumber);
            for (int j = 0; j < vexNumber; j++)
            {
                FBXVertex vex = partVextexes[j];
                Vector3 pos2 = vex.Position;
                xmesh.Append<Vector3>(pos2);
                xnormal.Append<Vector3>(vex.Normal ?? new Vector3());
                //
                if (vex.TexCoords.Count == 0)
                {
                    hasTexture = false; //throw new ApplicationException("Error: no texcoord existed in Mesh: " + part.Name + "! Exit.");
                }
                xtexture0.Append<Vector2>(new Vector2(vex.TexCoord.X, vex.TexCoord.Y));
                if (vex.TexCoords.Count > 1)
                {
                    if (j == 0)
                    {
                        int fvfcode = ((vex.TexCoords.Count - 1) << 8) | (int)VertexFormats.Position;
                        xfvf.Append<int>(fvfcode);
                        int dwcount = vexNumber * (vex.TexCoords.Count - 1) * 2;
                        xfvf.Append<int>(dwcount);
                    }
                    for (int k = 1; k < vex.TexCoords.Count; k++)
                    {
                        xfvf.Append<Vector2>(new Vector2(vex.TexCoords[k].X, vex.TexCoords[k].Y));
                    }
                }
                //
                if (HasRealColorValue(vex))
                {
                    hasColor = true;
                }
                xcolor.Append<int>(j);//color index
                xcolor.Append<Vector4>(vex.Color ?? new Vector4());
            }

            xmesh.Append<int>(indNumber / 3);
            xnormal.Append<int>(indNumber / 3);
            for (int j = 0; j < indNumber / 3; j++)
            {
                xmesh.Append<int>(3);
                xnormal.Append<int>(3);
                for (int k = 0; k < 3; k++)
                {
                    int vexindex = partIndices[j * 3 + k];
                    xmesh.Append<int>(vexindex);
                    xnormal.Append<int>(vexindex);
                }
            }

            XFileSaveData fmesh = f2.AddDataObject(XFileGuid.Mesh, "", Guid.Empty, xmesh.ToArray());
            fmesh.AddDataObject(XFileGuid.MeshNormals, "", Guid.Empty, xnormal.ToArray());
            if (hasTexture)
            {
                fmesh.AddDataObject(XFileGuid.MeshTextureCoords, "", Guid.Empty, xtexture0.ToArray());
                if (xfvf.Length > 0)
                {
                    fmesh.AddDataObject(FVFDataGuid, "", Guid.Empty, xfvf.ToArray());
                }
            }
            else
            {
                Console.WriteLine("Warning: no texcoord existed in Mesh: " + part.Name + ".");
            }
            if (hasColor)
            {
                fmesh.AddDataObject(XFileGuid.MeshVertexColors, "", Guid.Empty, xcolor.ToArray());
            }


            if (part.Material != null && !string.IsNullOrEmpty(part.Material.Texture))
            {
                XStream xmatlst = new XStream();
                xmatlst.Append<int>(1);
                xmatlst.Append<int>(indNumber / 3);
                for (int k = 0; k < indNumber / 3; k++)
                {
                    xmatlst.Append<int>(0);
                }

                XFileSaveData fmatlst = fmesh.AddDataObject(XFileGuid.MeshMaterialList, "", Guid.Empty, xmatlst.ToArray());
                ConvertMaterial(part.Material, fmatlst);
            }
            if (mesh.BoneInfo != null)
            {
                // ConvertSkinInfo(mesh, part, fmesh);
                ConvertSkinInfo(mesh, partVextexes, fmesh);
            }
        }

        private bool HasRealColorValue(FBXVertex vex)
        {
            bool has = false;
            if (!vex.Color.HasValue) return has;
            if (vex.Color.Value.X != 1)
            {
                has = true;
            }
            if (vex.Color.Value.Y != 1)
            {
                has = true;
            }
            if (vex.Color.Value.Z != 1)
            {
                has = true;
            }
            if (vex.Color.Value.W != 1)
            {
                has = true;
            }
            return has;

        }
        private void ConvertSkinInfo(FbxMesh mesh, FBXVertex[] vextexes, XFileSaveData fmesh)
        {
            using (XSkinInfo xskin = new XSkinInfo())
            {
                for (int j = 0; j < vextexes.Length; j++)
                {
                    FBXVertex vex = vextexes[j];
                    float testweight = 0;
                    for (int k = 0; k < vex.Weights.Count; k++)
                    {
                        FbxBoneWeight fbw = vex.Weights[k];
                        testweight += fbw.Weight;
                        xskin.AddVertex(fbw.BoneName, j, fbw.Weight);
                    }
                    if (testweight < 0.9f)
                    {
                        Console.WriteLine("Note: weight <0.9 in  V: " + j + " W:" + testweight);
                    }
                }
                xskin.SetBoneMatrix(mesh.BoneInfo);
                //skin
                Dictionary<string, XStream> xss = xskin.GetMeshSkinInfoBytes();

                XStream xskin1 = new XStream();
                xskin1.Append<short>(4);
                xskin1.Append<short>(4);
                xskin1.Append<short>((short)xss.Count);
                fmesh.AddDataObject(SkinHeaderGuid, "", Guid.Empty, xskin1.ToArray());
                //?
                foreach (string key in xss.Keys)
                {
                    XStream xskin2 = xss[key];
                    fmesh.AddDataObject(SkinWeightGuid, "", Guid.Empty, xskin2.ToArray());
                }
            }
        }

        private void ConvertNode(FbxSkeletalNode node, XFileSaveData frame)
        {
            frame.AddDataObject(XFileGuid.FrameTransformMatrix, "", Guid.Empty, XStream.ToBytes<Matrix>(node.TransformMatrix));

            if (node.Mesh != null)
            {
                if (!optDeleteMeshesWithoutSkinBeforeExporting || node.Mesh.BoneInfo != null)
                {
                    ConvertMesh(node.Mesh, frame);
                }
                //return;
            }
            foreach (FbxSkeletalNode child in node.Children)
            {
                XFileSaveData subframe = frame.AddDataObject(XFileGuid.Frame, XStream.ValidString(child.NodeName), Guid.Empty, XStream.ZeroBytes);
                ConvertNode(child, subframe);
            }

        }

        private void ConvertMaterial(FbxMaterial fbx, XFileSaveData fmatlst)
        {
            Vector4 diffuse = new Vector4(1, 1, 1, 1);
            float power = 0f;
            Vector3 specular = new Vector3(1, 1, 1);
            Vector3 emissive = new Vector3(0, 0, 0);
            XStream xmat = new XStream();
            if (fbx.DiffuseColor.HasValue)
            {
                diffuse = ToXVector(fbx.DiffuseColor, fbx.Alpha);
            }
            if (fbx.SpecularPower.HasValue)
            {
                power = fbx.SpecularPower.Value;
            }
            if (fbx.SpecularColor.HasValue)
            {
                specular = fbx.SpecularColor ?? new Vector3();
            }
            if (fbx.EmissiveColor.HasValue)
            {
                emissive = fbx.EmissiveColor ?? new Vector3();
            }
            xmat.Append<Vector4>(diffuse);
            xmat.Append<float>(power);
            xmat.Append<Vector3>(specular);
            xmat.Append<Vector3>(emissive);

            XFileSaveData fmat = fmatlst.AddDataObject(XFileGuid.Material, "", Guid.Empty, xmat.ToArray());
            string texturename = Path.GetFileName(fbx.Texture);
            string localurl = getTextureLocalURL(fbx.Texture);
            if (!string.IsNullOrEmpty(localurl) && !_cacheToCopy.ContainsKey(localurl))
            {
                _cacheToCopy.Add(localurl, texturename);
            }
            if (!string.IsNullOrEmpty(texturename) && !_usedTextures.Contains(texturename))
            {
                _usedTextures.Add(texturename);
            }
            fmat.AddDataObject(XFileGuid.TextureFilename, "", Guid.Empty, XStream.ToBytes(texturename));
        }
        private void ConvertMaterial(FbxMaterial fbx, XmlMaterialList xmllst, string meshName)
        {
            Vector4 diffuse = new Vector4(1, 1, 1, 1);
            float power = 0f;
            Vector3 specular = new Vector3(1, 1, 1);
            Vector3 emissive = new Vector3(0, 0, 0);
            if (fbx.DiffuseColor.HasValue)
            {
                diffuse = ToXVector(fbx.DiffuseColor, fbx.Alpha);
            }
            if (fbx.SpecularPower.HasValue)
            {
                power = fbx.SpecularPower.Value;
            }
            if (fbx.SpecularColor.HasValue)
            {
                specular = fbx.SpecularColor ?? new Vector3();
            }
            if (fbx.EmissiveColor.HasValue)
            {
                emissive = fbx.EmissiveColor ?? new Vector3();
            }

            XmlMaterial xmat = new XmlMaterial();
            xmat.faceColor = diffuse;
            xmat.power = power;
            xmat.specularColor = specular;
            xmat.emissiveColor = emissive;

            string texturename = Path.GetFileName(fbx.Texture);
            string localurl = getTextureLocalURL(fbx.Texture);
            if (!string.IsNullOrEmpty(localurl) && !_cacheToCopy.ContainsKey(localurl))
            {
                _cacheToCopy.Add(localurl, texturename);
            }
            if (!string.IsNullOrEmpty(texturename) && !_usedTextures.Contains(texturename))
            {
                _usedTextures.Add(texturename);
            }
            //fmat.AddDataObject(XFileGuid.TextureFilename, "", Guid.Empty, XStream.ToBytes(texturename));
            xmat.textureFileName = texturename;
            xmat.frameMeshName = meshName;
            xmllst.materialList.Add(xmat);
        }
        //public static Vector3 ToXVector(XNA.Vector3? xna)
        //{
        //    Vector3 vect = new Vector3();

        //    XNA.Vector3 vector = XNA.Vector3.Zero;
        //    if (xna.HasValue)
        //    {
        //        vector.X = xna.Value.X;
        //        vector.Y = xna.Value.Y;
        //        vector.Z = xna.Value.Z;
        //    }
        //    else
        //    {
        //        throw new ApplicationException("Zero");
        //    }
        //    vect.X = vector.X;
        //    vect.Y = vector.Y;
        //    vect.Z = vector.Z;
        //    return vect;
        //}
        public static Vector4 ToXVector(Vector3? xna, float? alpha)
        {
            Vector4 vect = new Vector4();

            Vector3 vector = new Vector3();
            float al = 1f;
            if (xna.HasValue)
            {
                vector.X = xna.Value.X;
                vector.Y = xna.Value.Y;
                vector.Z = xna.Value.Z;
            }
            if (alpha.HasValue)
            {
                al = alpha.Value;
            }
            vect.X = vector.X;
            vect.Y = vector.Y;
            vect.Z = vector.Z;
            vect.W = al;
            return vect;
        }
        //public static Vector4 ToXVector(XNA.Vector4? xna)
        //{
        //    Vector4 vect = new Vector4();

        //    XNA.Vector4 vector = XNA.Vector4.Zero;
        //    float al = 1f;
        //    if (xna.HasValue)
        //    {
        //        vector.X = xna.Value.X;
        //        vector.Y = xna.Value.Y;
        //        vector.Z = xna.Value.Z;
        //        vector.W = xna.Value.W;
        //    }

        //    vect.X = vector.X;
        //    vect.Y = vector.Y;
        //    vect.Z = vector.Z;
        //    vect.W = vector.W;
        //    return vect;
        //}
        public static float[] GetMatrixBytes(Matrix mx)
        {
            float[] result = new float[16];
            result[0] = mx.M11;
            result[1] = mx.M12;
            result[2] = mx.M13;
            result[3] = mx.M14;
            result[4] = mx.M21;
            result[5] = mx.M22;
            result[6] = mx.M23;
            result[7] = mx.M24;
            result[8] = mx.M31;
            result[9] = mx.M32;
            result[10] = mx.M33;
            result[11] = mx.M34;
            result[12] = mx.M41;
            result[13] = mx.M42;
            result[14] = mx.M43;
            result[15] = mx.M44;
            return result;
        }


        #region support for convertion

        string getTextureLocalURL(string textureFullName)
        {
            if (string.IsNullOrEmpty(textureFullName)) return "";
            string texturename = Path.GetFileName(textureFullName);
            if (File.Exists(textureFullName))
            {
                return textureFullName;
            }
            string localurl = Path.Combine(_sourceDir, texturename);
            if (File.Exists(localurl))
            {
                return localurl;
            }
            if (!_notExists.Contains(texturename))
            {
                Console.WriteLine("Can not find texture:" + texturename);
                _notExists.Add(texturename);
            }

            return "";
        }

        #endregion //support for convertion


    }
}
