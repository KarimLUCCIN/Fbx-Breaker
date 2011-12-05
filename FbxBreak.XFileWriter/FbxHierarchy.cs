using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Collections;
using System.IO;
using Microsoft.DirectX;

namespace FbxBreak.XFileWriter
{
    public class FbxAnimationKey
    {
        int time = -1;
        public int Time
        {
            get { return time; }
            set { time = value; }
        }
        Matrix matrix = Matrix.Identity;
        public Matrix Matrix
        {
            get { return matrix; }
            set { matrix = value; }
        }
    }
    //for one part of animation
    public class FbxAnimationPart
    {
        string name = "";
        public string ObjectName
        {
            set { name = value; }
            get { return name; }
        }
        List<FbxAnimationKey> list = new List<FbxAnimationKey>();
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FbxAnimationKey> Children
        {
            get { return list; }
            set { list = value; }
        }

        int timestart = -1;
        int timestop = -1;
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PlayStart
        {
            get { return timestart; }
            set { timestart = value; }
        }
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PlayStop
        {
            get { return timestop; }
            set { timestop = value; }
        }
    }
    //for one set of animation
    public class FbxAnimationTake
    {
        string name = "";
        public string Name
        {
            set { name = value; }
            get { return name; }
        }

        List<FbxAnimationPart> list = new List<FbxAnimationPart>();
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FbxAnimationPart> Children
        {
            get { return list; }
            set { list = value; }
        }

        int timestart = -1;
        int timestop = -1;
        public int PlayStart
        {
            get { return timestart; }
            set { timestart = value; }
        }
        public int PlayStop
        {
            get { return timestop; }
            set { timestop = value; }
        }
        private int fps = 4800;
        public int FPS
        {
            get { return fps; }
            set { fps = value; }
        }
        // for save bone hierarchy
        public FbxBoneInfo BoneInfo = null;
    }

    public class FbxAnimationCollection
    {
        private List<FbxAnimationTake> list = new List<FbxAnimationTake>();

        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FbxAnimationTake> Children
        {
            set { list = value; }
            get { return list; }
        }

        FbxSkinnedAnimation owner = null;
        public void SetOwner(FbxSkinnedAnimation Owner)
        {
            owner = Owner;
        }
        public FbxSkinnedAnimation GetOwner()
        {
            return owner;
        }
        public FbxAnimationTake GetNamedAnimation(string name)
        {
            if (list.Count == 0) return null;
            FbxAnimationTake anim = list[0];
            foreach (FbxAnimationTake subanim in list)
            {
                if (subanim.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    anim = subanim;
                    break;
                }
            }
            return anim;
        }
        public StringCollection GetNameList()
        {
            StringCollection sc = new StringCollection();
            foreach (FbxAnimationTake subanim in list)
            {
                sc.Add(subanim.Name);
            }
            return sc;
        }

        public void AddChild(FbxAnimationTake value)
        {
            //throw new Exception("The method or operation is not implemented.");

            Children.Add(value);
        }


        internal static FbxAnimationCollection Load(string url)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
    public class FbxSkinnedAnimation
    {
        private int fps = 4800;
        public int FPS
        {
            get { return fps; }
            set { fps = value; }
        }

        #region Skeletal hierarchy
        // hosting all skeletal information in a tree structure.
        FbxSkeletalNode skeletal = null;
        Hashtable skeletalmap = new Hashtable();
        //bool initiated = false;
        public FbxSkinnedAnimation()
        {
            //rorate Animation 90 degree around X arix

            Matrix wor = new Matrix();
            //wor.Rotate(new Quaternion(new Vector3D(1, 0, 0), 90));
            world = wor;
        }

        string skeletalurl = "";
        public string SkeletalSource
        {
            get { return skeletalurl; }
            set
            {
                try
                {
                    if (!this.skeletalurl.Equals(value))
                    {
                        skeletalmap.Clear();
                        this.skeletalurl = value;
                        string url = value;
                        if (!Path.IsPathRooted(url))
                        {
                            url = CurrentBaseUrl + Path.DirectorySeparatorChar + url;
                        }
                        FbxSkeletalNode node = FbxSkeletalNode.Load(url);
                        this.SetSkeletalHierarchy(node);
                    }
                }
                catch (Exception)
                {
                    //Logger.Debug(this, "" + ex.ToString());
                    this.skeletalurl = "";
                }
            }
        }

        static string currentBaseUrl = "";
        public static string CurrentBaseUrl
        {
            set { currentBaseUrl = value; }
            get { return currentBaseUrl; }
        }

        Matrix world = Matrix.Identity;
        public Matrix World
        {
            get { return world; }
            set { world = value; }
        }

        public void SetSkeletalSourceValue(string url)
        {
            skeletalurl = url;
        }
        public FbxSkeletalNode GetSkeletalHierarchy()
        {
            return skeletal;
        }

        public FbxSkeletalNode SetSkeletalHierarchy(FbxSkeletalNode root)
        {
            FbxSkeletalNode oldroot = this.skeletal;

            if (root != null)
            {
                this.skeletal = root;
                this.skeletal.IsRoot = true;
                this.skeletal.SetOwner(this);
                this.UpdateSkeletalMap(this.skeletal);
            }
            return oldroot;
        }

        public FbxSkeletalNode FindFbxSkeletalNode(string NodeName)
        {
            this.Validate();
            FbxSkeletalNode node = this.skeletalmap[NodeName] as FbxSkeletalNode;
            return node;
        }
        public Matrix GetCombinedMatrix(string NodeName)
        {
            Matrix Matrix = Matrix.Identity;
            FbxSkeletalNode node = FindFbxSkeletalNode(NodeName);
            if (node != null)
                Matrix = node.CombinedMatrix;
            return Matrix;
        }
        public void AddSkeletalTreeNode(FbxSkeletalNode child, FbxSkeletalNode parent)
        {
            parent.AddChildren(child);
            // this.skeletalmap[child.NodeName] = child;
        }

        public void RecordFbxSkeletalNode(FbxSkeletalNode node)
        {
            this.skeletalmap[node.NodeName] = node;
            node.SetOwner(this);
        }
        public StringCollection GetSkeletalNameList()
        {
            StringCollection scol = new StringCollection();
            foreach (string key in this.skeletalmap.Keys)
            {
                scol.Add(key);
            }
            return scol;
        }



        public bool UpdateFrameMatrices(double progress)
        {
            //world= Matrix.Identity as default

            return this.UpdateFrameMatrices(world, progress);
        }
        public bool UpdateFrameMatrices(Matrix parentMatrix, double progress)
        {
            this.Validate();
            if (this.skeletal != null)
            {
                this.skeletal.UpdateFrameMatrices(parentMatrix, progress);
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// recursive update
        /// </summary>
        /// <param name="node"></param>
        public void UpdateSkeletalMap(FbxSkeletalNode node)
        {
            if (node != null)
            {
                this.RecordFbxSkeletalNode(node);
                foreach (FbxSkeletalNode subnode in node.Children)
                {
                    UpdateSkeletalMap(subnode);
                }
            }
        }

        #endregion //Skeletal hierarchy

        #region Animation File
        string animationname = "";
        public string AnimationName
        {
            get { return animationname; }
            set
            {
                animationname = value;
                UpdateAnimationSource();
            }
        }
        string animationurl = "";
        FbxAnimationCollection animationcol = null;
        public string AnimationSource
        {
            get { return animationurl; }
            set
            {
                try
                {
                    if (!this.animationurl.Equals(value))
                    {
                        animationurl = value;
                        string url = value;
                        if (!Path.IsPathRooted(url))
                        {
                            url = CurrentBaseUrl + Path.DirectorySeparatorChar + url;
                        }
                        animationcol = FbxAnimationCollection.Load(url);
                        UpdateAnimationSource();
                    }
                }
                catch (Exception)
                {
                    //Logger.Debug(this, "" + ex.ToString());
                    this.animationurl = "";
                }
                //
            }
        }
        public void SetAnimationSourceValue(string url)
        {
            animationurl = url;
        }
        public void SetAnimation(FbxAnimationCollection acol)
        {
            animationcol = acol;
            UpdateAnimationSource();
        }
        public FbxAnimationCollection GetAnimation()
        {
            return animationcol;
        }

        public void UpdateAnimationSource()
        {
            if (!this.Validate()) return;
            animationcol.SetOwner(this);
            FbxAnimationTake animset = animationcol.GetNamedAnimation(animationname);
            if (animset != null)
            {
                if (this.skeletal != null)
                {
                    this.skeletal.ClearAnimation();
                    //initiated = true;
                }
                foreach (FbxAnimationPart anim in animset.Children)
                {
                    anim.PlayStart = animset.PlayStart;
                    anim.PlayStop = animset.PlayStop;
                    this.fps = animset.FPS;
                    //
                    RecordAnimation(anim);
                }
            }
        }

        public void RecordAnimation(FbxAnimationPart anim)
        {
            if (this.skeletalmap.ContainsKey(anim.ObjectName))
            {
                FbxSkeletalNode node = this.skeletalmap[anim.ObjectName] as FbxSkeletalNode;
                node.AttachFbxAnimationPart(anim);
            }
        }
        private bool Validate()
        {
            bool val = false;
            if (this.skeletal == null && !string.IsNullOrEmpty(this.skeletalurl))
            {
                this.SkeletalSource = this.skeletalurl;
            }
            if (this.animationcol == null && !string.IsNullOrEmpty(this.animationurl))
            {
                this.AnimationSource = this.animationurl;
            }
            if (this.skeletal != null && this.animationcol != null)
            {
                val = true;
            }
            return val;
        }
        #endregion // Animation File
    }


    public class FbxBoneWeight
    {
        public string BoneName;
        public float Weight;
        public FbxBoneWeight(string boneName, float weight)
        {
            BoneName = boneName;
            Weight = weight;
        }
        public bool Equals(FbxBoneWeight obj)
        {
            bool equal = true;
            if (!this.BoneName.Equals(obj.BoneName))
            {
                equal = false;
                return equal;
            }
            if (this.Weight != obj.Weight)
            {
                equal = false;
                return equal;
            }
            return equal;
        }
        public override string ToString()
        {
            string temp = BoneName;
            temp = temp + Weight;
            return temp;
        }
    }
    public class FbxBoneWeightCollection : List<FbxBoneWeight>
    {
        public bool Equals(FbxBoneWeightCollection obj)
        {
            bool equal = true;
            if (this.Count != obj.Count)
            {
                equal = false;
                return equal;
            }
            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].Equals(obj[i]))
                {
                    equal = false;
                    break;
                }
            }
            return equal;
        }
        public override string ToString()
        {
            string temp = "";
            foreach (FbxBoneWeight bw in this)
            {
                temp = temp + bw;
            }
            return temp;
        }
    }

    public class FbxMaterial
    {
        public Vector3? DiffuseColor;
        public Vector3? EmissiveColor;
        public float? Alpha;
        public Vector3? SpecularColor;
        public float? SpecularPower;
        public string Texture = "";
        public static bool Compare(FbxMaterial ma, FbxMaterial mb)
        {
            bool equal = true;
            if (mb == null)
            {
                equal = false;
                return equal;
            }
            if (!ma.DiffuseColor.Equals(mb.DiffuseColor))
            {
                equal = false;
            }
            if (!ma.EmissiveColor.Equals(mb.EmissiveColor))
            {
                equal = false;
            }
            if (!ma.Alpha.Equals(mb.Alpha))
            {
                equal = false;
            }
            if (!ma.SpecularColor.Equals(mb.SpecularColor))
            {
                equal = false;
            }
            if (!ma.SpecularPower.Equals(mb.SpecularPower))
            {
                equal = false;
            }
            if (ma.Texture != mb.Texture)
            {
                equal = false;
            }
            return equal;

        }
    }

    public class FBXVertex
    {
        public Vector3 Position;
        public Vector2 TexCoord
        {
            get
            {
                if (TexCoords.Count > 0)
                {
                    return TexCoords[0];
                }
                return new Vector2();
            }
            set
            {
                if (TexCoords.Count == 0)
                {
                    TexCoords.Add(value);
                }
                else
                {
                    TexCoords[0] = value;
                }
            }
        }
        public List<Vector2> TexCoords = new List<Vector2>();
        public FbxBoneWeightCollection Weights;
        public Vector4? Color;
        public Vector3? Normal;
        public override string ToString()
        {
            string temp = "" + Position;
            temp = temp + "Texs_";
            for (int i = 0; i < TexCoords.Count; i++)
            {
                temp = temp + TexCoords[i];
            }
            temp = temp + Weights;
            temp = temp + Color;
            temp = temp + Normal;
            return temp;
        }
    }
    public class FBXVertexContent
    {
        public int VertexIndex;
        public Vector2 TexCoord
        {
            get
            {
                if (TexCoords.Count > 0)
                {
                    return TexCoords[0];
                }
                return new Vector2();
            }
            set
            {
                if (TexCoords.Count == 0)
                {
                    TexCoords.Add(value);
                }
                else
                {
                    TexCoords[0] = value;
                }
            }
        }
        public List<Vector2> TexCoords = new List<Vector2>();
        public FbxBoneWeightCollection Weights;
        public Vector3? Normal;
        public Vector4? Color;
        public static bool IsEqual(FBXVertexContent vex1, FBXVertexContent vex2)
        {
            bool equal = true;
            if (vex1.VertexIndex != vex2.VertexIndex)
            {
                equal = false;
            }
            if (!vex1.TexCoords.Equals(vex2.TexCoords))
            {
                equal = false;
            }
            //
            if (!vex1.Weights.Equals(vex2.Weights))
            {
                equal = false;
            }
            if (!vex1.Color.Equals(vex2.Color))
            {
                equal = false;
            }
            return equal;
        }
    }


    public class FbxMeshPart
    {
        public int BaseVertex = 0;
        public int NumVertices = 0;
        public int PrimitiveCount = 0;
        public int StartIndex = 0;
        public string Name = null;
        public object Tag = null;

        //public string TextureName = null;
        public FbxMaterial Material = null;
    }

    public class FbxBoneInfo
    {
        public StringCollection AllBoneNames = new StringCollection();
        //linkname,linkmatrix
        public Dictionary<string, Matrix> LinkMatrixes = new Dictionary<string, Matrix>();
        FbxSkeletalNode _root = null;
        public FbxSkeletalNode Root
        {
            set
            {
                _root = value;
                _root.IsRoot = true;
                skeletalmap.Clear();
                this.UpdateSkeletalMap(this._root);
            }
            get { return _root; }
        }

        Hashtable skeletalmap = new Hashtable();
        private void RecordFbxSkeletalNode(FbxSkeletalNode node)
        {
            this.skeletalmap[node.NodeName] = node;
        }
        private void UpdateSkeletalMap(FbxSkeletalNode node)
        {
            if (node != null)
            {
                this.RecordFbxSkeletalNode(node);
                foreach (FbxSkeletalNode subnode in node.Children)
                {
                    UpdateSkeletalMap(subnode);
                }
            }
        }
        public FbxSkeletalNode FindFbxSkeletalNode(string NodeName)
        {
            FbxSkeletalNode node = this.skeletalmap[NodeName] as FbxSkeletalNode;
            return node;
        }

        public List<FbxSkeletalNode> GetAllBoneNodes()
        {
            List<FbxSkeletalNode> nodelst = new List<FbxSkeletalNode>();
            foreach (string bone in this.AllBoneNames)
            {
                nodelst.Add(FindFbxSkeletalNode(bone));
            }
            return nodelst;
        }

    }

    public class FbxMesh
    {
        //public IList<Vector3> Positions = new List<Vector3>();
        //public IList<FBXVertex> VertexIndexes = new List<FBXVertex>();
        public IList<FBXVertex> Vertexes = new List<FBXVertex>();
        public IList<int> Indexes = new List<int>();
        public IList<FbxMeshPart> MeshParts = new List<FbxMeshPart>();
        public FbxBoneInfo BoneInfo = null;
        public bool HasGeometry = false;
        public string Name = "";
        public FbxSkeletalNode RefNode = null;
        //public BoundingSphere Bounding =new BoundingSphere();
        public Matrix BoneTransform = Matrix.Identity;
    }

    public class FbxMeshBuilder
    {
        public string MeshName;
        FbxMesh mesh = null;
        FbxMeshPart part = null;

        //
        IList<Vector3> _Positions = null;// new List<Vector3>();
        IList<FBXVertexContent> _VertexIndexes = null;// new List<FBXVertexContent>();

        int StartIndex = 0;

        //Vector2 currentTexture = Vector2.Zero;
        List<Vector2> currentLayerTextures = null;
        FbxBoneWeightCollection currentBone = null;
        Vector3? currentNormal = null;
        Vector4? currentColor = null;
        FbxMaterial currentMat = null;
        protected FbxMeshBuilder(string meshName)
        {
            MeshName = meshName;

            _Positions = new List<Vector3>();
            _VertexIndexes = new List<FBXVertexContent>();
            part = null;
            StartIndex = 0;
            //currentTexture = Vector2.Zero;
            currentLayerTextures = new List<Vector2>();
            currentBone = null;
            currentColor = null;
            currentMat = null;

            mesh = new FbxMesh();
            mesh.Name = MeshName;

        }
        public static FbxMeshBuilder StartMesh(string meshName)
        {
            return new FbxMeshBuilder(meshName);
        }

        public int CreatePosition(float x, float y, float z)
        {
            _Positions.Add(new Vector3(x, y, z));
            //NumVertices++;
            return _Positions.Count - 1;
        }

        public void SetMaterial(FbxMaterial mat)
        {
            if (currentMat != null)
            {
                if (FbxMaterial.Compare(currentMat, mat))
                {
                    return;
                }
            }
            if (part != null)
            {
                part.PrimitiveCount = (StartIndex - part.StartIndex) / 3;
                mesh.MeshParts.Add(part);
            }
            part = new FbxMeshPart();
            part.StartIndex = StartIndex;
            part.Material = mat;
            currentMat = mat;
        }
        public void SetVertexColor(Vector4? Color)
        {
            currentColor = Color;
        }
        public void SetVertexNormal(Vector3? Normal)
        {
            currentNormal = Normal;
        }
        public void SetVertexTextureCords(Vector2 texture)
        {
            //currentTexture = texture;
            SetVertexTextureCords(0, texture);
        }
        public void SetVertexTextureCords(int layer, Vector2 texture)
        {
            if (currentLayerTextures.Count <= layer)
            {
                currentLayerTextures.Add(texture);
            }

            if (currentLayerTextures.Count != layer + 1) throw new ApplicationException("not ordered texture  coord layer number != currentLayerTextures.Count!");


        }
        public void SetVertexBoneWeights(FbxBoneWeightCollection bonewc)
        {
            currentBone = bonewc;
        }
        public void AddTriangleVertex(int vertexindex)
        {
            if (currentMat == null)
            {
                FbxMaterial mat = new FbxMaterial();
                mat.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
                mat.Alpha = 1.0f;
                this.SetMaterial(mat);
            }

            FBXVertexContent vex = new FBXVertexContent();
            vex.VertexIndex = vertexindex;
            vex.TexCoords.AddRange(currentLayerTextures);
            currentLayerTextures.Clear();
            //

            vex.Weights = currentBone;
            vex.Color = currentColor;
            vex.Normal = currentNormal;
            _VertexIndexes.Add(vex);
            //
            StartIndex++;
            //
            mesh.HasGeometry = true;

        }
        public FbxMesh FinishMesh()
        {
            if (part != null)
            {
                part.PrimitiveCount = (StartIndex - part.StartIndex) / 3;
                mesh.MeshParts.Add(part);
            }
            else
            {
                return null;
            }
            IList<Vector3> pos = _Positions;
            //Dictionary<string, FBXVertex> pos2 = new Dictionary<string, FBXVertex>();
            List<string> pos2 = new List<string>();
            List<FBXVertex> vv2 = new List<FBXVertex>();

            mesh.Indexes = new List<int>();
            bool hasNormal = false;
            int partcount = 0;
            //List<FbxAnimationCollection> pos2 = new List<Vector3>();
            for (int k = 0; k < mesh.MeshParts.Count; k++)// each (FbxMeshPart p in mesh.MeshParts)
            {
                FbxMeshPart p = mesh.MeshParts[k];
                p.Name = mesh.Name + (partcount++);
                p.BaseVertex = pos2.Count;
                Dictionary<string, FBXVertex> test = new Dictionary<string, FBXVertex>();
                for (int i = 0; i < p.PrimitiveCount * 3; i++)
                {
                    FBXVertexContent vex1 = _VertexIndexes[i + p.StartIndex];
                    //int index = vex1.VertexIndex;
                    FBXVertex vex2 = new FBXVertex();
                    vex2.Position = pos[vex1.VertexIndex];
                    vex2.TexCoords = vex1.TexCoords;
                    vex2.Color = vex1.Color;
                    vex2.Normal = vex1.Normal;
                    if (vex1.Normal.HasValue)
                    {
                        hasNormal = true;
                    }
                    vex2.Weights = vex1.Weights;
                    string key = "part" + k + "_" + vex2.ToString();
                    if (test.ContainsKey(key))
                    {
                        mesh.Indexes.Add(pos2.IndexOf(key));
                    }
                    else
                    {
                        mesh.Indexes.Add(pos2.Count);

                        pos2.Add(key);
                        vv2.Add(vex2);
                        test.Add(key, vex2);
                    }
                }
                //
                p.NumVertices = pos2.Count - p.BaseVertex;
            }
            mesh.Vertexes = vv2;

            // without material case
            if (pos2.Count == 0)
            {
                foreach (Vector3 vect in pos)
                {
                    FBXVertex item = new FBXVertex();
                    item.Position = vect;
                    mesh.Vertexes.Add(item);
                }
                foreach (FBXVertexContent vi in _VertexIndexes)
                {
                    mesh.Indexes.Add(vi.VertexIndex);
                }
            }
            if (!hasNormal)
            {
                calculateTriangleNormals(mesh.Indexes, mesh.Vertexes);
            }
            if (_Positions != null && _Positions.Count > 0)
            {
                // mesh.Bounding = BoundingSphere.CreateFromPoints(_Positions);
            }

            return mesh;
        }
        public static void SwapWindingOrder(FbxMesh mesh)
        {
            foreach (FbxMeshPart p in mesh.MeshParts)
            {
                for (int i = 0; i < p.PrimitiveCount * 3; i += 3)
                {
                    int index = mesh.Indexes[i + p.StartIndex];
                    mesh.Indexes[i + p.StartIndex] = mesh.Indexes[i + 2 + p.StartIndex];
                    mesh.Indexes[i + 2 + p.StartIndex] = index;
                    //interchange between vex and vex2;
                }
            }
        }
        public static void calculateTriangleNormals(IList<int> indices, IList<FBXVertex> vertices)
        {
            // IndirectPositionCollection collection1 = vertices[0].Positions;
            // VertexChannel<int> channel1 = vertices.PositionIndices;
            for (int j = 0; j < vertices.Count; j++)
            {
                vertices[j].Normal = new Vector3();
            }

            for (int i = 0; i < indices.Count; i += 3)
            {
                Vector3 Vector4 = vertices[indices[i]].Position;// collection1[indices[i]];
                Vector3 vector1 = vertices[indices[i + 1]].Position;// collection1[indices[i + 1]];
                Vector3 Vector3 = vertices[indices[i + 2]].Position; //collection1[indices[i + 2]];
                Vector3 Vector2 = SafeNormalize(Vector3.Cross(Vector3 - vector1, vector1 - Vector4));
                for (int j = 0; j < 3; j++)
                {
                    //vertexNormals[channel1[indices[i + j]]] += Vector2;
                    vertices[indices[i + j]].Normal += Vector2;
                }
            }
            for (int j = 0; j < vertices.Count; j++)
            {
                vertices[j].Normal = SafeNormalize(vertices[j].Normal ?? new Vector3());
            }
        }
        static Vector3 SafeNormalize(Vector3 value)
        {
            float single1 = value.Length();
            if (single1 == 0.00F)
            {
                return new Vector3();
            }
            return value * (1.0f / single1);
        }


    }

    // [ContentProperty("Content")]
    public class FbxSkeletalNode
    {
        FbxMesh mesh = null;
        public FbxMesh Mesh
        {
            set
            {
                mesh = value;
                mesh.RefNode = this;
            }
            get { return mesh; }
        }

        IList<FbxMesh> meshMirror = null;
        public IList<FbxMesh> MeshesCache
        {
            set { meshMirror = value; }
            get { return meshMirror; }
        }

        Matrix transformMatrix = Matrix.Identity;
        public Matrix TransformMatrix
        {
            set { transformMatrix = value; }
            get { return transformMatrix; }
        }
        Matrix combinedMatrix = Matrix.Identity;
        public Matrix CombinedMatrix
        {
            //set { combinedMatrix = value; }
            get { return combinedMatrix; }
        }

        bool isRoot = false;
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsRoot
        {
            get { return isRoot; }
            set { isRoot = value; }
        }

        bool isMesh = false;
        public bool IsMesh
        {
            get { return isMesh; }
            set { isMesh = true; }
        }

        int oldKeyNumber = -1;
        public void UpdateFrameMatrices(Matrix parentMatrix, double progress)
        {
            SetProgess(progress);
            //if (this.keynumber != oldKeyNumber)
            {
                this.combinedMatrix = this.TransformMatrix * parentMatrix;
            }
            oldKeyNumber = this.keynumber;

            foreach (FbxSkeletalNode node in Children)
            {
                node.UpdateFrameMatrices(this.combinedMatrix, progress);
            }
        }
        public void UpdateFrameMatrices(Matrix parentMatrix)
        {
            this.combinedMatrix = this.TransformMatrix * parentMatrix;

            foreach (FbxSkeletalNode node in Children)
            {
                node.UpdateFrameMatrices(this.combinedMatrix);
            }
        }

        public void ClearAnimation()
        {
            FbxAnimationPart = new FbxAnimationPart();
            keynumber = -1;
            keycount = FbxAnimationPart.Children.Count;

            keystart = -1;
            keystop = -1;

            foreach (FbxSkeletalNode node in Children)
            {
                node.ClearAnimation();
            }
        }

        FbxSkinnedAnimation animationowner = null;
        public FbxSkinnedAnimation GetOwner()
        {
            return animationowner;

        }
        public void SetOwner(FbxSkinnedAnimation Owner)
        {
            this.animationowner = Owner;
        }
        int keynumber = -1;
        int keycount = 0;
        public int GetKeyCount()
        {
            return keycount;
        }
        public int GetKeyNumber()
        {
            return keynumber;
        }
        int keystart = -1;
        int keystop = -1;
        public int getTotalRawTime()
        {
            return keystop - keystart;
        }
        public double getTotalRealTime()
        {
            double rawtime = getTotalRawTime();

            return rawtime / animationowner.FPS;
        }
        public void SetProgess(double progress)
        {
            if (keycount > 0)
            {
                if (keynumber == -1)
                {
                    keynumber = 0;
                }
                double time = keystart + (keystop - keystart) * progress;
                double refertime = FbxAnimationPart.Children[keynumber].Time;
                if (time > refertime)
                {
                    for (int i = keynumber; i < keycount; i++)
                    {
                        FbxAnimationKey key = FbxAnimationPart.Children[i];
                        if (key.Time > time)
                        {
                            keynumber = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = keynumber; i >= 0; i--)
                    {
                        FbxAnimationKey key = FbxAnimationPart.Children[i];
                        if (key.Time < time)
                        {
                            keynumber = i;
                            break;
                        }
                    }
                }
                //
                FbxAnimationKey timedkey = FbxAnimationPart.Children[keynumber];
                ///if (FbxAnimationKey.keyType == 4)
                //{
                this.transformMatrix = timedkey.Matrix;//XMatrix4x4.ToMatrix(timedkey.tfkeys.values);
                //}
            }
        }

        FbxAnimationPart FbxAnimationPart = null;
        public void AttachFbxAnimationPart(FbxAnimationPart part)
        {
            FbxAnimationPart = part;
            keynumber = -1;
            keycount = part.Children.Count;
            FbxAnimationKey start = FbxAnimationPart.Children[0];
            FbxAnimationKey stop = FbxAnimationPart.Children[keycount - 1];
            keystart = start.Time;
            keystop = stop.Time;
            if (part.PlayStart != -1)
            {
                keystart = part.PlayStart;
            }
            if (part.PlayStop != -1)
            {
                keystop = part.PlayStop;
            }
        }
        public FbxAnimationPart GetFbxAnimationPart()
        {
            return FbxAnimationPart;
        }

        public FbxSkeletalNode()
            : this("", null, Matrix.Identity)
        {
        }
        public FbxSkeletalNode(string Name, FbxSkinnedAnimation Owner, Matrix TransformMatrix)
        {
            this.nodename = Name;
            this.animationowner = Owner;
            this.hierarchy = 0;
            this.transformMatrix = TransformMatrix;
        }

        int hierarchy = -1;
        public int GetHierarchy()
        {
            return hierarchy;
        }
        public int SetHierarchy(int value)
        {
            int back = value;
            this.hierarchy = value;
            return back;
        }
        string nodename = "";
        public string NodeName
        {
            get { return nodename; }
            set { nodename = value; }
        }
        FbxSkeletalNode parent = null;
        public FbxSkeletalNode Parent
        {
            get { return parent; }
        }
        List<FbxSkeletalNode> children = new List<FbxSkeletalNode>();
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FbxSkeletalNode> Children
        {
            get { return this.children; }
            //set { this.children = value; }
        }
        public bool AddChildren(FbxSkeletalNode node)
        {
            this.children.Add(node);
            node.parent = this;
            node.SetHierarchy(this.GetHierarchy() + 1);
            if (this.animationowner != null)
            {
                this.animationowner.RecordFbxSkeletalNode(node);
            }
            return true;
        }


        internal static FbxSkeletalNode Load(string url)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
