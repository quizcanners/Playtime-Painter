using System;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.MeshEditing
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


    public enum GridPlane
    {
        xz = 0,
        xy = 1,
        zy = 2
    }

    [ExecuteInEditMode]
    public class PP_GridNavigator : PainterSystemMono
    {
        public static PP_GridNavigator Instance
        {
            get
            {
                if (_inst) return _inst;
                if (!ApplicationIsQuitting)
                {
                    _inst = PainterCamera.Inst
                        .GetComponentInChildren<PP_GridNavigator>(); //(GridNavigator)FindObjectOfType<GridNavigator>();
                    if (_inst) return _inst;
                    try
                    {
                        var prefab = Resources.Load(PainterDataAndConfig.PREFABS_RESOURCE_FOLDER + "/grid") as GameObject;
                        _inst = Instantiate(prefab).GetComponent<PP_GridNavigator>();
                        _inst.transform.parent = PainterCamera.Inst.transform;
                        _inst.name = "grid";
                        _inst.gameObject.hideFlags = HideFlags.DontSave;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                }
                else _inst = null;

                return _inst;
            }
        }
        private static PP_GridNavigator _inst;


        public static Vector3 LatestMouseRaycastHit;
        public static Vector3 LatestMouseToGridProjection;
        [HideInInspector] public GridPlane CurrentPlane = GridPlane.xz;


        [SerializeField] public Material vertexPointMaterial;
        [SerializeField] protected GameObject vertPrefab;
        [SerializeField] public MarkerWithText[] vertices;
        [SerializeField] public MarkerWithText pointedVertex;
        [SerializeField] public MarkerWithText selectedVertex;
        [SerializeField] protected MeshRenderer dot;
        [SerializeField] protected MeshRenderer rendy;


        private const KeyCode verticalPlanesKey = KeyCode.Z;
        private const KeyCode horisontalPlaneKey = KeyCode.X;

        private static Plane _xzPlane = new Plane(Vector3.up, 0);
        private static Plane _zyPlane = new Plane(Vector3.right, 0);
        private static Plane _xyPlane = new Plane(Vector3.forward, 0);
        private static readonly Quaternion XGrid = Quaternion.Euler(new Vector3(0, 90, 0));
        private static readonly Quaternion ZGrid = Quaternion.Euler(new Vector3(0, 0, 0));
        private static readonly Quaternion YGrid = Quaternion.Euler(new Vector3(90, 0, 0));

        public static readonly string ToolTip =
         "{0} {1}: Toggle vertical grid orientations {0} {2}: Set grid horizontal {0} Scroll wheel can change grid projection while in play mode {0}"
             .F(pegi.EnvironmentNl, verticalPlanesKey, horisontalPlaneKey);


        private readonly ShaderProperty.FloatValue _dxProp = new ShaderProperty.FloatValue("_dx");
        private readonly ShaderProperty.FloatValue _dyProp = new ShaderProperty.FloatValue("_dy");
        private readonly ShaderProperty.FloatValue _sizeProp = new ShaderProperty.FloatValue("_Size");
        private readonly ShaderProperty.VectorValue _dotPositionProperty = new ShaderProperty.VectorValue("_GridDotPosition");

        public static void MoveToPointedPosition()
        {
            RaycastHit hit;

            if (RaycastMouse(out hit))
            {
                LatestMouseRaycastHit = hit.point;
                LatestMouseToGridProjection = LatestMouseRaycastHit;
            }
        }

        public static bool RaycastMouse(out RaycastHit hit) => Physics.Raycast(EditorInputManager.GetScreenMousePositionRay(TexMGMT.MainCamera), out hit);
        
        public void DeactivateVertices()
        {

            for (var i = 0; i < MeshEditorManager.Inst.verticesShowMax; i++)
            {
                var v = vertices[i];

                if (v == null)
                    Debug.LogError("Got Null in vertices");
                else if (!v.go)
                    Debug.LogError("Game object in vertices is null");
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
            EnabledUpdate(rendy, gridEn);
            EnabledUpdate(dot, dotEn);
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
            var ray = EditorInputManager.GetScreenMousePositionRay(TexMGMT.MainCamera);
            float rayDistance;
            return plane.Raycast(ray, out rayDistance) ? ray.GetPoint(rayDistance) : Vector3.zero;
        }

        public Vector3 PlaneToWorldVector(Vector2 v2)
        {
            switch (CurrentPlane)
            {
                case GridPlane.xy: return new Vector3(v2.x, v2.y, 0); //Mirror.z = 1; break;
                case GridPlane.xz: return new Vector3(v2.x, 0, v2.y); //
                case GridPlane.zy: return new Vector3(0, v2.y, v2.x); //
                default: return Vector3.zero;
            }

        }

        public Vector2 InPlaneVector(Vector3 f)
        {
            switch (CurrentPlane)
            {
                case GridPlane.xy: return new Vector2(f.x, f.y); //Mirror.z = 1; break;
                case GridPlane.xz: return new Vector2(f.x, f.z); //
                case GridPlane.zy: return new Vector2(f.z, f.y); //
                default: return Vector3.zero;
            }
        }

        public float PerpendicularToPlaneVector(Vector3 f)
        {
            switch (CurrentPlane)
            {
                case GridPlane.xy: return f.z; //Mirror.z = 1; break;
                case GridPlane.xz: return f.y; //new Vector2(f.x, f.z); //
                case GridPlane.zy: return f.x; //new Vector2(f.z, f.y); //
                default: return 0;
            }

        }

        public Vector3 GetGridPerpendicularVector()
        {
            var mirror = Vector3.zero;

            switch (CurrentPlane)
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

            switch (CurrentPlane)
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

        public Vector3 ProjectToGrid(Vector3 src)
        {
            var pos = LatestMouseToGridProjection;

            switch (CurrentPlane)
            {
                case GridPlane.xy:
                    return new Vector3(src.x, src.y, pos.z);
                case GridPlane.xz:
                    return new Vector3(src.x, pos.y, src.z);
                case GridPlane.zy:
                    return new Vector3(pos.x, src.y, src.z);
            }

            return Vector3.zero;
        }

        private void ClosestAxis(bool horToo)
        {
            var ang = CurrentViewTransform().rotation.x;
            if (!horToo || (ang < 35 || ang > 300))
            {
                var x = AngleClamp(XGrid);
                var z = AngleClamp(ZGrid);

                CurrentPlane = x <= z ? GridPlane.zy : GridPlane.xy;
            }
            else CurrentPlane = GridPlane.xz;

        }

        private void ScrollsProcess(float delta)
        {
            var before = CurrentPlane;
            if (delta > 0)
                switch (CurrentPlane)
                {
                    case GridPlane.xy:
                        CurrentPlane = GridPlane.zy;
                        break;
                    case GridPlane.xz:
                        ClosestAxis(false);
                        break;
                    case GridPlane.zy:
                        CurrentPlane = GridPlane.xy;
                        break;
                }
            else if (delta < 0)
                CurrentPlane = GridPlane.xz;

            if (before != CurrentPlane && MeshEditorManager.target)
                MeshEditorManager.MeshTool.OnGridChange();

        }

        public void UpdatePositions()
        {

            var cfg = TexMgmtData;

            if (!cfg)
                return;

            var showGrid = MeshEditorManager.target.NeedsGrid() || TexMGMT.FocusedPainter.NeedsGrid();

            SetEnabled(showGrid, cfg.snapToGrid && showGrid);

            if (!showGrid)
                return;

            if (cfg.gridSize <= 0) cfg.gridSize = 1;

            switch (CurrentPlane)
            {
                case GridPlane.xy:
                    rendy.transform.rotation = ZGrid;
                    break;
                case GridPlane.xz:
                    rendy.transform.rotation = YGrid;
                    break;
                case GridPlane.zy:
                    rendy.transform.rotation = XGrid;
                    break;
            }

            _xzPlane.distance = -LatestMouseToGridProjection.y;
            _xyPlane.distance = -LatestMouseToGridProjection.z;
            _zyPlane.distance = -LatestMouseToGridProjection.x;

            var hit = Vector3.zero;

            switch (CurrentPlane)
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

                switch (CurrentPlane)
                {
                    case GridPlane.xy:

                        LatestMouseToGridProjection.x = hit.x;
                        LatestMouseToGridProjection.y = hit.y;

                        break;
                    case GridPlane.xz:
                        LatestMouseToGridProjection.x = hit.x;
                        LatestMouseToGridProjection.z = hit.z;
                        break;
                    case GridPlane.zy:

                        LatestMouseToGridProjection.z = hit.z;
                        LatestMouseToGridProjection.y = hit.y;
                        break;
                }
            }

            var tf = transform;
            var dotTf = dot.transform;
            var rndTf = rendy.transform;

            tf.position = LatestMouseToGridProjection + Vector3.one * 0.01f;

            var position = tf.position;

            _dotPositionProperty.GlobalValue = new Vector4(LatestMouseToGridProjection.x, LatestMouseToGridProjection.y, LatestMouseToGridProjection.z);

            dotTf.rotation = CurrentViewTransform().rotation;

            var cam = CurrentViewTransform();

            var dist = Mathf.Max(0.1f, (cam.position - position).magnitude * 2);

            dotTf.localScale = Vector3.one * (dist / 64f);
            rndTf.localScale = new Vector3(dist, dist, dist);

            float scale = !cfg.snapToGrid ? Mathf.Max(1, Mathf.ClosestPowerOfTwo((int) (dist / 8))) : cfg.gridSize;

            var dx = CurrentPlane != GridPlane.zy ? position.x : -position.z;

            var dy = CurrentPlane != GridPlane.xz ? position.y : position.z;

            dx -= Mathf.Round(dx / scale) * scale;
            dy -= Mathf.Round(dy / scale) * scale;

            var mat = rendy.sharedMaterial;

            mat.Set(_dxProp, dx / dist)
                .Set(_dyProp, dy / dist)
                .Set(_sizeProp, dist / scale);

            if (MeshEditorManager.target)
                MeshMGMT.UpdateLocalSpaceMousePosition();
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


            if (!MeshEditorManager.target && TexMgmtData)
                UpdatePositions();

        }

        private void OnEnable() => _inst = this;

        public void FeedEvent(Event e)
        {

            if (!rendy || !rendy.enabled)
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

            if (EditorInputManager.GetMouseButtonDown(2))
            {
                RaycastHit hit;
                if (Physics.Raycast(EditorInputManager.GetScreenMousePositionRay(TexMGMT.MainCamera), out hit))
                    LatestMouseToGridProjection = hit.point;
            }
        }

        public void InitializeIfNeeded(int verticesShowMax) 
        {
            if (!vertPrefab)
                vertPrefab = Resources.Load(PainterDataAndConfig.PREFABS_RESOURCE_FOLDER + "/vertex") as GameObject;

            if ((vertices == null) || (vertices.Length == 0) || (!vertices[0].go))
            {
                vertices = new MarkerWithText[verticesShowMax];

                for (int i = 0; i < verticesShowMax; i++)
                {
                    MarkerWithText v = new MarkerWithText();
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
            "vertexPointMaterial".write(vertexPointMaterial);
            pegi.nl();
            "vertexPrefab".edit(ref vertPrefab).nl();
            "pointedVertex".edit(ref pointedVertex.go).nl();
            "SelectedVertex".edit(ref selectedVertex.go).nl();
        }

    }
}