using System;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.MeshEditing
{

#pragma warning disable IDE0018 // Inline variable declaration

 

    [ExecuteInEditMode]
    [AddComponentMenu("Playtime Painter/Grid XYZ Navigator")]
    public class GridNavigator : PainterSystemMono
    {
     
        [HideInInspector] 

        public Material vertexPointMaterial;
        public MarkerWithText[] vertices;
        public MarkerWithText pointedVertex;
        public MarkerWithText selectedVertex;
        [SerializeField] protected GameObject vertPrefab;
        [SerializeField] protected MeshRenderer dotPointed;
        [SerializeField] protected MeshRenderer gridRenderer;


        private const KeyCode verticalPlanesKey = KeyCode.Z;
        private const KeyCode horisontalPlaneKey = KeyCode.X;

        private static Plane _xzPlane = new (Vector3.up, 0);
        private static Plane _zyPlane = new (Vector3.right, 0);
        private static Plane _xyPlane = new (Vector3.forward, 0);
        private static readonly Quaternion XGrid = Quaternion.Euler(new Vector3(0, 90, 0));
        private static readonly Quaternion ZGrid = Quaternion.Euler(new Vector3(0, 0, 0));
        private static readonly Quaternion YGrid = Quaternion.Euler(new Vector3(90, 0, 0));

        public static readonly string ToolTip =
         "{0} {1}: Toggle vertical grid orientations {0} {2}: Set grid horizontal {0} Scroll wheel can change grid projection while in play mode {0}"
             .F(pegi.EnvironmentNl, verticalPlanesKey, horisontalPlaneKey);


        private readonly ShaderProperty.FloatValue _dxProp = new("_dx");
        private readonly ShaderProperty.FloatValue _dyProp = new("_dy");
        private readonly ShaderProperty.FloatValue _sizeProp = new("_Size");
        private readonly ShaderProperty.VectorValue _dotPositionProperty = new("_GridDotPosition");

        public override string InspectedCategory => nameof(PainterComponent);

        public static void MoveToPointedPosition()
        {
            RaycastHit hit;

            if (RaycastMouse(out hit))
            {
                MeshPainting.LatestMouseRaycastHit = hit.point;
                MeshPainting.LatestMouseToGridProjection = MeshPainting.LatestMouseRaycastHit;
            }
        }

        public static bool RaycastMouse(out RaycastHit hit) => Physics.Raycast(PlaytimePainter_EditorInputManager.GetScreenMousePositionRay(Painter.Camera.MainCamera), out hit);
        
        public void DeactivateVertices()
        {

            for (var i = 0; i < Painter.MeshManager.verticesShowMax; i++)
            {
                var v = vertices[i];

                if (v == null)
                    Debug.LogError(QcLog.IsNull(v, nameof(DeactivateVertices))); //"Got Null in vertices");
                else if (!v.go)
                    Debug.LogError(QcLog.IsNull(v.go, nameof(DeactivateVertices)));
                else
                    v.go.SetActive(false);
            }

            if (pointedVertex.go)
                pointedVertex.go.SetActive(false);

            if (selectedVertex.go)
                selectedVertex.go.SetActive(false);
        }

        public void EnabledUpdate(Renderer c, bool setTo)
        {
            //There were some update when enabled state is changed
            if (c && c.enabled != setTo)
                c.enabled = setTo;
        }

        public void SetEnabled(bool gridEn, bool dotEn)
        {
            EnabledUpdate(gridRenderer, gridEn);
            EnabledUpdate(dotPointed, dotEn);
        }

        private float AngleClamp(Quaternion ang)
        {
            var res = Quaternion.Angle(CurrentViewTransform().rotation, ang);
            if (res > 90)
                res = 180 - res;
            return res;
        }

        public float AngGridToCamera(Vector3 hitPos)
        {
            var ang = (Vector3.Angle(GetGridPerpendicularVector(), hitPos - CurrentViewTransform().position));
            if (ang > 90)
                ang = 180 - ang;
            return ang;
        }

        private static Vector3 MouseToPlane(Plane plane)
        {
            var ray = PlaytimePainter_EditorInputManager.GetScreenMousePositionRay(Painter.Camera.MainCamera);
            float rayDistance;
            return plane.Raycast(ray, out rayDistance) ? ray.GetPoint(rayDistance) : Vector3.zero;
        }

        public Vector3 PlaneToWorldVector(Vector2 v2)
        {
            return MeshPainting.CurrentPlane switch
            {
                GridPlane.xy => new Vector3(v2.x, v2.y, 0),//Mirror.z = 1; break;
                GridPlane.xz => new Vector3(v2.x, 0, v2.y),//
                GridPlane.zy => new Vector3(0, v2.y, v2.x),//
                _ => Vector3.zero,
            };
        }

        public Vector2 InPlaneVector(Vector3 f)
        {
            return MeshPainting.CurrentPlane switch
            {
                GridPlane.xy => new Vector2(f.x, f.y),//Mirror.z = 1; break;
                GridPlane.xz => new Vector2(f.x, f.z),//
                GridPlane.zy => new Vector2(f.z, f.y),//
                _ => Vector3.zero,
            };
        }

        public float PerpendicularToPlaneVector(Vector3 f)
        {
            return MeshPainting.CurrentPlane switch
            {
                GridPlane.xy => f.z,//Mirror.z = 1; break;
                GridPlane.xz => f.y,//new Vector2(f.x, f.z); //
                GridPlane.zy => f.x,//new Vector2(f.z, f.y); //
                _ => 0,
            };
        }

        public Vector3 GetGridPerpendicularVector()
        {
            var mirror = Vector3.zero;

            switch (MeshPainting.CurrentPlane)
            {
                case GridPlane.xy:
                    mirror.z = 1;
                    break;
                case GridPlane.xz:
                    mirror.y = 1;
                    break;
                case GridPlane.zy:
                    mirror.x = 1;
                    break;
            }

            return mirror;
        }

        public Vector3 GetGridMaskVector()
        {
            var mirror = Vector3.one;

            switch (MeshPainting.CurrentPlane)
            {
                case GridPlane.xy:
                    mirror.z = 0;
                    break;
                case GridPlane.xz:
                    mirror.y = 0;
                    break;
                case GridPlane.zy:
                    mirror.x = 0;
                    break;
            }

            return mirror;
        }

        

        private void ClosestAxis(bool horToo)
        {
            var ang = CurrentViewTransform().rotation.x;
            if (!horToo || (ang < 35 || ang > 300))
            {
                var x = AngleClamp(XGrid);
                var z = AngleClamp(ZGrid);

                MeshPainting.CurrentPlane = x <= z ? GridPlane.zy : GridPlane.xy;
            }
            else MeshPainting.CurrentPlane = GridPlane.xz;

        }

        private void ScrollsProcess(float delta)
        {
            if (!CurrentViewTransform())
                return;


            var before = MeshPainting.CurrentPlane;
            if (delta > 0)
                switch (MeshPainting.CurrentPlane)
                {
                    case GridPlane.xy:
                        MeshPainting.CurrentPlane = GridPlane.zy;
                        break;
                    case GridPlane.xz:
                        ClosestAxis(false);
                        break;
                    case GridPlane.zy:
                        MeshPainting.CurrentPlane = GridPlane.xy;
                        break;
                }
            else if (delta < 0)
                MeshPainting.CurrentPlane = GridPlane.xz;

            if (before != MeshPainting.CurrentPlane && MeshPainting.target)
                MeshEditorManager.MeshTool.OnGridChange();

        }

        public void UpdatePositions()
        {

            var cfg = Painter.Data;

            if (!cfg)
                return;

            var showGrid = MeshPainting.target.NeedsGrid() || Painter.FocusedPainter.NeedsGrid();

            SetEnabled(showGrid, cfg.snapToGrid && showGrid);

            if (!showGrid)
                return;

            if (cfg.gridSize <= 0) cfg.gridSize = 1;

            switch (MeshPainting.CurrentPlane)
            {
                case GridPlane.xy:
                    gridRenderer.transform.rotation = ZGrid;
                    break;
                case GridPlane.xz:
                    gridRenderer.transform.rotation = YGrid;
                    break;
                case GridPlane.zy:
                    gridRenderer.transform.rotation = XGrid;
                    break;
            }

            var LatestMouseToGridProjection = MeshPainting.LatestMouseToGridProjection;

            _xzPlane.distance = -LatestMouseToGridProjection.y;
            _xyPlane.distance = -LatestMouseToGridProjection.z;
            _zyPlane.distance = -LatestMouseToGridProjection.x;

            var hit = Vector3.zero;

            switch (MeshPainting.CurrentPlane)
            {
                case GridPlane.xy:
                    hit = MouseToPlane(_xyPlane);
                    break;
                case GridPlane.xz:
                    hit = MouseToPlane(_xzPlane);
                    break;
                case GridPlane.zy:
                    hit = MouseToPlane(_zyPlane);
                    break;
            }

            if (cfg.snapToGrid)
                hit = QcMath.RoundDiv(hit, cfg.gridSize);

            if (hit != Vector3.zero)
            {

                switch (MeshPainting.CurrentPlane)
                {
                    case GridPlane.xy:

                        MeshPainting.LatestMouseToGridProjection.x = hit.x;
                        MeshPainting.LatestMouseToGridProjection.y = hit.y;

                        break;
                    case GridPlane.xz:
                        MeshPainting.LatestMouseToGridProjection.x = hit.x;
                        MeshPainting.LatestMouseToGridProjection.z = hit.z;
                        break;
                    case GridPlane.zy:

                        MeshPainting.LatestMouseToGridProjection.z = hit.z;
                        MeshPainting.LatestMouseToGridProjection.y = hit.y;
                        break;
                }
            }

            var tf = transform;
            var dotTf = dotPointed.transform;
            var rndTf = gridRenderer.transform;

            tf.position = MeshPainting.LatestMouseToGridProjection + Vector3.one * 0.01f;

            var position = tf.position;

            _dotPositionProperty.GlobalValue = new Vector4(LatestMouseToGridProjection.x, LatestMouseToGridProjection.y, LatestMouseToGridProjection.z);

            dotTf.rotation = CurrentViewTransform().rotation;

            var cam = CurrentViewTransform();

            var dist = Mathf.Max(0.1f, (cam.position - position).magnitude * 2);

            dotTf.localScale = Vector3.one * (dist / 64f);
            rndTf.localScale = new Vector3(dist, dist, dist);

            float scale = !cfg.snapToGrid ? Mathf.Max(1, Mathf.ClosestPowerOfTwo((int) (dist / 8))) : cfg.gridSize;

            var dx = MeshPainting.CurrentPlane != GridPlane.zy ? position.x : -position.z;

            var dy = MeshPainting.CurrentPlane != GridPlane.xz ? position.y : position.z;

            dx -= Mathf.Round(dx / scale) * scale;
            dy -= Mathf.Round(dy / scale) * scale;

            var mat = gridRenderer.sharedMaterial;

            mat.Set(_dxProp, dx / dist)
                .Set(_dyProp, dy / dist)
                .Set(_sizeProp, dist / scale);

            if (MeshPainting.target)
                MeshPainting.UpdateLocalSpaceMousePosition();
        }

        private void Update()
        {

            if (!enabled)
                return;

            if (Application.isPlaying)
            {
                if (Input.GetKeyDown(verticalPlanesKey))
                    ScrollsProcess(1);
                else if (Input.GetKeyDown(horisontalPlaneKey))
                    ScrollsProcess(-1);
                else
                    ScrollsProcess(Input.GetAxis("Mouse ScrollWheel"));
            }


            if (!MeshPainting.target && Painter.Data)
                UpdatePositions();

        }

        public void FeedEvent(Event e)
        {

            if (!gridRenderer || !gridRenderer.enabled)
                return;

            if (e.isMouse)
                UpdatePositions();


            if (e.type == EventType.KeyDown)
            {

                bool isHorisontal = e.keyCode == verticalPlanesKey;

                if (isHorisontal || e.keyCode == horisontalPlaneKey)
                {
                    ScrollsProcess(isHorisontal ? 1 : -1);
                    UpdatePositions();
                    e.Use();
                }
            }

            if (PlaytimePainter_EditorInputManager.GetMouseButtonDown(2))
            {
                RaycastHit hit;
                if (Physics.Raycast(PlaytimePainter_EditorInputManager.GetScreenMousePositionRay(Painter.Camera.MainCamera), out hit))
                    MeshPainting.LatestMouseToGridProjection = hit.point;
            }
        }

        public void InitializeIfNeeded(int verticesShowMax) 
        {
            if (!vertPrefab)
                vertPrefab = Resources.Load(SO_PainterDataAndConfig.PREFABS_RESOURCE_FOLDER + "/vertex") as GameObject;

            if ((vertices == null) || (vertices.Length == 0) || (!vertices[0].go))
            {
                vertices = new MarkerWithText[verticesShowMax];

                for (int i = 0; i < verticesShowMax; i++)
                {
                    MarkerWithText v = new();
                    vertices[i] = v;
                    v.go = UnityEngine.Object.Instantiate(vertPrefab, transform, true);
                    v.Init();
                }
            }

            pointedVertex.Init();
            selectedVertex.Init();
        }

        public override void Inspect()
        {
            "vertexPointMaterial".PegiLabel().Write(vertexPointMaterial);
            pegi.Nl();
            "vertexPrefab".PegiLabel().Edit(ref vertPrefab).Nl();
            "pointedVertex".PegiLabel().Edit(ref pointedVertex.go).Nl();
            "SelectedVertex".PegiLabel().Edit(ref selectedVertex.go).Nl();
        }

    }
}