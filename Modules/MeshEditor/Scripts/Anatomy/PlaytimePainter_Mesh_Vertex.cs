using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace PainterTool.MeshEditing
{
    public partial class PainterMesh
    {
        public class Vertex : PainterClassCfg
        {
           // protected PainterComponent Painter => MeshPainting.target;

            public BoneWeight boneWeight;
            public Color color;
            public int uvIndex;
            public int finalIndex;
            public int groupIndex;

            public bool hasVertex;
            public List<Triangle> triangles = new();
            public Vertex myLastCopy;
            public MeshPoint meshPoint;

            #region Constructors

            private void AssignToPoint(MeshPoint nPoint, Vertex toCopyFrom = null)
            {
                meshPoint = nPoint;

                var verts = nPoint.vertices;

                if (toCopyFrom == null && verts.Count > 0)
                    toCopyFrom = verts[0];

                if (toCopyFrom != null)
                {
                    boneWeight = toCopyFrom.boneWeight;
                    color = toCopyFrom.color;
                    groupIndex = toCopyFrom.groupIndex;
                }
                else color = Color.white;

                nPoint.vertices.Add(this);
            }

            public Vertex()
            {
                meshPoint = MeshPoint.currentlyDecoded;
                color = Color.white;
            }

            public Vertex(Vertex other)
            {
                AssignToPoint(other.meshPoint, other);
                uvIndex = other.uvIndex;
            }

            public Vertex(MeshPoint newVertex)
            {
                AssignToPoint(newVertex);

                if (meshPoint.sharedUVs.Count == 0)
                    SetUvIndexBy(Vector2.one * 0.5f, Vector2.one * 0.5f);
                else
                    uvIndex = 0;
            }

            public Vertex(MeshPoint newVertex, Vector2 uv0)
            {
                AssignToPoint(newVertex);

                SetUvIndexBy(uv0, Vector2.zero);
            }

            public Vertex(MeshPoint newVertex, Vector2 uv0, Vector2 uv1)
            {
                AssignToPoint(newVertex);
                SetUvIndexBy(uv0, uv1);
            }

            public Vertex(MeshPoint newVertex, Vertex other)
            {

                AssignToPoint(newVertex, other);
                SetUvIndexBy(other.GetUvSet(0), other.GetUvSet(1));
            }

            public Vertex(MeshPoint newVertex, CfgData data)
            {
                AssignToPoint(newVertex);
                this.Decode(data);
            }

            #endregion

            public bool SetColor(Color col)
            {
                if (col.Equals(color)) return false;
                color = col;
                return true;
            }

            public bool SameAsLastFrame => this == EditedMesh.lastFramePointedUv;

            public Vector3 LocalPos => meshPoint.localPos;

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("i", finalIndex)
                    .Add_IfNotZero("cg", groupIndex)
                    .Add_IfNotZero("uvi", uvIndex)
                    .Add_IfNotWhite("col", color)
                    .Add("bw", boneWeight);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "i":
                        data.ToInt(ref finalIndex);
                        MeshData.decodedEditableMesh.uvsByFinalIndex[finalIndex] = this;
                        break;
                    case "cg": data.ToInt(ref groupIndex); break;
                    case "uvi": data.ToInt(ref uvIndex); break;
                    case "col": color = data.ToColor(); break;
                    case "bw": boneWeight = data.ToBoneWeight(); break;
                }
            }

            #endregion

            public Vertex GetConnectedUVinVertex(MeshPoint other)
            {
                foreach (var t in triangles)
                    if (t.Includes(other))
                        return (t.GetByVertex(other));

                return null;
            }

            public List<Triangle> GetTrianglesFromLine(Vertex other)
            {
                var lst = new List<Triangle>();

                foreach (var t in triangles)
                    if (t.Includes(other))
                        lst.Add(t);

                return lst;
            }

            public void RunDebug()
            {

            }

            public void AssignToNewVertex(MeshPoint vp)
            {
                var myUv = meshPoint.sharedUVs[uvIndex];
                meshPoint.vertices.Remove(this);
                meshPoint = vp;
                meshPoint.vertices.Add(this);
                SetUvIndexBy(myUv);
            }

            #region UV MGMT

            public Vector2 EditedUv
            {
                get { return meshPoint.sharedUVs[uvIndex][Painter.MeshManager.EditedUV]; }
                set { SetUvIndexBy(value); }
            }

            public Vector2 SharedEditedUv
            {
                get { return meshPoint.sharedUVs[uvIndex][Painter.MeshManager.EditedUV]; }
                set { meshPoint.sharedUVs[uvIndex][Painter.MeshManager.EditedUV] = value; }
            }

            public Vector2 GetUvSet(int ind) => meshPoint.sharedUVs[uvIndex][ind];

            public Vector2 this[UvSetIndex uvSetIndex]
            {
                get
                {
                    return meshPoint.sharedUVs[uvIndex][uvSetIndex.index];
                }
            }

            public bool SameUv(Vertex other, int uvSetId) => other.GetUvSet(uvSetId) == GetUvSet(uvSetId);

            public bool SameUv(Vector2 uv, Vector2 uv1) =>
                (uv - GetUvSet(0)).magnitude < 0.0000001f && (uv1 - GetUvSet(1)).magnitude < 0.0000001f;

            public void SetUvIndexBy(Vector2[] uvs) => uvIndex = meshPoint.GetUvsDataIndexFor(uvs[0], uvs[1]).index;

            public void SetUvIndexBy(Vector2 uv0, Vector2 uv1) => uvIndex = meshPoint.GetUvsDataIndexFor(uv0, uv1).index;

            public bool SetUvIndexBy(Vector2 uvEdited)
            {
                var uv0 = Painter.MeshManager.EditedUV == 0 ? uvEdited : GetUvSet(0);
                var uv1 = Painter.MeshManager.EditedUV == 1 ? uvEdited : GetUvSet(1);

                var index = meshPoint.GetUvsDataIndexFor(uv0, uv1);

                if (index.index == uvIndex) 
                    return false;

                uvIndex = index.index;
                return true;

            }

            public void UVto01Space() => EditedUv = EditedUv.To01Space();

            public bool SameTileAs(Vertex other) => EditedUv.Floor().Equals(other.EditedUv.Floor());

            #endregion

            public bool ConnectedTo(MeshPoint other)
            {
                foreach (var t in triangles)
                    if (t.Includes(other))
                        return true;

                return false;
            }

            public bool ConnectedTo(Vertex other)
            {
                foreach (var t in triangles)
                    if (t.Includes(other))
                        return true;

                return false;
            }

            public void SetColor_OppositeTo(ColorChanel chan)
            {
                for (var i = 0; i < 3; i++)
                {
                    var c = (ColorChanel)i;
                    c.SetValueOn(ref color, c == chan ? 0 : 1);
                }
            }

            public void FlipChanel(ColorChanel chan)
            {
                var val = chan.GetValueFrom(color);
                val = (val > 0.9f) ? 0 : 1;
                chan.SetValueOn(ref color, val);
            }

            private ColorChanel GetZeroChanelIfOne(ref int count)
            {
                count = 0;
                var ch = ColorChanel.A;
                for (var i = 0; i < 3; i++)
                    if (((ColorChanel)i).GetValueFrom(color) > 0.9f)
                        count++;
                    else
                        ch = (ColorChanel)i;

                return ch;
            }

            public ColorChanel GetZeroChanel_AifNotOne()
            {
                var count = 0;

                var ch = GetZeroChanelIfOne(ref count);

                if (count == 2)
                    return ch;

                foreach (var u in meshPoint.vertices)
                    if (u != this)
                    {
                        ch = GetZeroChanelIfOne(ref count);
                        if (count == 2) return ch;
                    }

                return ColorChanel.A;
            }

            public static implicit operator int(Vertex d) => d.finalIndex;

        }
    }
}
