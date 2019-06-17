using UnityEngine;
using System;
using PlaytimePainter;
using QuizCannersUtilities;
using PlayerAndEditorGUI;

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


public enum Gridside { xz, xy, zy }

[ExecuteInEditMode]
public class GridNavigator : PainterSystemMono {

    public static GridNavigator Inst()  {
        if (_inst) return _inst;
        if (!ApplicationIsQuitting)
        {
            _inst = PainterCamera.Inst.GetComponentInChildren<GridNavigator>();//(GridNavigator)FindObjectOfType<GridNavigator>();
            if (_inst) return _inst;
            try
            {
                _inst = Instantiate((Resources.Load("prefabs/grid") as GameObject)).GetComponent<GridNavigator>();
                _inst.transform.parent = PainterCamera.Inst.transform;
                _inst.name = "grid";
                _inst.gameObject.hideFlags = HideFlags.DontSave;
            }
            catch (Exception ex)
            {
                Debug.Log("Couldn't load a prefab. If this happened once it's ok. " + ex.ToString());
            }

        }
        else _inst = null;
        return _inst;
    }

    private Transform Camera => gameObject.TryGetCameraTransform(TexMGMT.MainCamera);
    
    public Material vertexPointMaterial;
    public GameObject vertPrefab;
    public MarkerWithText[] vertices;
    public MarkerWithText pointedVertex;
    public MarkerWithText selectedVertex;

    private static GridNavigator _inst;

    private static Plane _xzPlane = new Plane(Vector3.up, 0);
    private static Plane _zyPlane = new Plane(Vector3.right, 0);
    private static Plane _xyPlane = new Plane(Vector3.forward, 0);

    private static readonly Quaternion XGrid = Quaternion.Euler(new Vector3(0, 90, 0));
    private static readonly Quaternion ZGrid = Quaternion.Euler(new Vector3(0, 0, 0));
    private static readonly Quaternion YGrid = Quaternion.Euler(new Vector3(90, 0, 0));
    
    public static Vector3 collisionPos;
 
    public static Vector3 onGridPos;
    [HideInInspector]
    public Gridside gSide = Gridside.xz;
    public MeshRenderer dot;
    public MeshRenderer rendy;

    private readonly ShaderProperty.VectorValue _dotPositionProperty = new ShaderProperty.VectorValue("_GridDotPosition");
    
    public void DeactivateVertices() {

        for (var i = 0; i < MeshManager.Inst.verticesShowMax; i++)
        {
            var v = vertices[i];
            
            if (v == null)
                Debug.LogError("Got Null in vertices");
            else
            if (!v.go)
                Debug.LogError("Game object in vertices is null");
            else
                v.go.SetActive(false);
        }

        if (pointedVertex.go)
            pointedVertex.go.SetActive(false);

        if (selectedVertex.go)
            selectedVertex.go.SetActive(false);
    }

    public void SetEnabled(bool gridEn, bool dotEn) {
        rendy.EnabledUpdate(gridEn);
            dot.EnabledUpdate(dotEn);
    }

    private float AngleClamp(Quaternion ang) {
        var res = Quaternion.Angle(Camera.rotation, ang);
        if (res > 90)
            res = 180 - res;
        return res;
    }

    public float AngGridToCamera(Vector3 hitPos)
    {
        var ang = (Vector3.Angle(GetGridPerpendicularVector(), hitPos - Camera.position));
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

    public Vector3 PlaneToWorldVector(Vector2 v2) {
        switch (gSide)
        {
            case Gridside.xy: return new Vector3(v2.x, v2.y, 0); //Mirror.z = 1; break;
            case Gridside.xz: return new Vector3(v2.x, 0, v2.y); //
            case Gridside.zy: return new Vector3(0, v2.y, v2.x); //
            default: return Vector3.zero;
        }

    }

    public Vector2 InPlaneVector(Vector3 f)
    {
        switch (gSide)
        {
            case Gridside.xy: return new Vector2(f.x, f.y); //Mirror.z = 1; break;
            case Gridside.xz: return new Vector2(f.x, f.z); //
            case Gridside.zy: return new Vector2(f.z, f.y); //
            default: return Vector3.zero;
        }
    }
    
    public float PerpendicularToPlaneVector(Vector3 f)
    {
        switch (gSide)
        {
            case Gridside.xy: return f.z; //Mirror.z = 1; break;
            case Gridside.xz: return f.y; //new Vector2(f.x, f.z); //
            case Gridside.zy: return f.x; //new Vector2(f.z, f.y); //
            default: return 0;
        }
        
    }

    public Vector3 GetGridPerpendicularVector()
    {
        var mirror = Vector3.zero;

        switch (gSide)
        {
            case Gridside.xy: mirror.z = 1; break;
            case Gridside.xz: mirror.y = 1; break;
            case Gridside.zy: mirror.x = 1; break;
        }
        return mirror;
    }

    public Vector3 GetGridMaskVector()
    {
        var mirror = Vector3.one;

        switch (gSide)
        {
            case Gridside.xy: mirror.z = 0; break;
            case Gridside.xz: mirror.y = 0; break;
            case Gridside.zy: mirror.x = 0; break;
        }

        return mirror;
    }

    public Vector3 ProjectToGrid(Vector3 src)
    {
        var pos = onGridPos;

        switch (gSide)
        {
            case Gridside.xy:
                return new Vector3(src.x, src.y, pos.z);
            case Gridside.xz:
                return new Vector3(src.x, pos.y, src.z);
            case Gridside.zy:
                return new Vector3(pos.x, src.y, src.z);
        }
        return Vector3.zero;
    }

    private void ClosestAxis(bool horToo)
    {
        var ang = Camera.rotation.x;
        if (!horToo || (ang < 35 || ang > 300))
        {
            var x = AngleClamp(XGrid);
            var z = AngleClamp(ZGrid);

            gSide = x <= z ? Gridside.zy : Gridside.xy;
        }
        else gSide = Gridside.xz;

    }

    private void ScrollsProcess(float delta) {
        var before = gSide;
        if (delta > 0)   
            switch (gSide) {
                case Gridside.xy: gSide = Gridside.zy; break;
                case Gridside.xz: ClosestAxis(false); break;
                case Gridside.zy: gSide = Gridside.xy; break;
            }
        else if (delta < 0)
            gSide = Gridside.xz;

        if (before != gSide && MeshManager.target)
            MeshMGMT.MeshTool.OnGridChange();

    }
    
    public void UpdatePositions() {

        var m = MeshMGMT;
        var cfg = TexMgmtData;

        if (!cfg)
            return;

        var showGrid = MeshManager.target.NeedsGrid() || TexMGMT.focusedPainter.NeedsGrid(); 

        SetEnabled(showGrid, cfg.snapToGrid && showGrid);

        if (!showGrid)
            return;

        if (cfg.gridSize <= 0) cfg.gridSize = 1;

        switch (gSide)
        {
            case Gridside.xy: rendy.transform.rotation = ZGrid; break;
            case Gridside.xz: rendy.transform.rotation = YGrid; break;
            case Gridside.zy: rendy.transform.rotation = XGrid; break;
        }

        _xzPlane.distance = -onGridPos.y;
        _xyPlane.distance = -onGridPos.z;
        _zyPlane.distance = -onGridPos.x;

        var hit = Vector3.zero;
        
        switch (gSide)    {
            case Gridside.xy:   hit = MouseToPlane(_xyPlane);           break;
            case Gridside.xz:   hit = MouseToPlane(_xzPlane);           break;
            case Gridside.zy:   hit = MouseToPlane(_zyPlane);           break;
        }

        if (cfg.snapToGrid)
            hit = QcMath.RoundDiv(hit, cfg.gridSize);

        if (hit != Vector3.zero)  {

            switch (gSide)
            {
                case Gridside.xy:

                    onGridPos.x = hit.x;
                    onGridPos.y = hit.y;

                    break;
                case Gridside.xz:
                    onGridPos.x = hit.x;
                    onGridPos.z = hit.z;
                    break;
                case Gridside.zy:

                    onGridPos.z = hit.z;
                    onGridPos.y = hit.y;
                    break;
            }
        }

        var tf = transform;
        var dotTf = dot.transform;
        var rndTf = rendy.transform;
        
        tf.position = onGridPos+Vector3.one*0.01f;
        
        var position = tf.position;
        
        _dotPositionProperty.GlobalValue = new Vector4(onGridPos.x, onGridPos.y, onGridPos.z);

        dotTf.rotation = Camera.rotation;

        var cam = Camera;

        var dist = Mathf.Max(0.1f, (cam.position - position).magnitude * 2);

        dotTf.localScale = Vector3.one * (dist / 64f);
        rndTf.localScale = new Vector3(dist, dist, dist);

        float scale = !cfg.snapToGrid ? Mathf.Max(1, Mathf.ClosestPowerOfTwo((int)(dist / 8))) : cfg.gridSize;
        
        var dx = gSide != Gridside.zy ? position.x : -position.z;

        var dy = gSide != Gridside.xz ? position.y : position.z;
        
        dx -= Mathf.Round(dx / scale) * scale;
        dy -= Mathf.Round(dy / scale) * scale;

        var mat = rendy.sharedMaterial;
        
        mat.Set(_dxProp, dx / dist)
            .Set(_dyProp, dy / dist)
            .Set(_sizeProp, dist / scale);

        if (MeshManager.target)
            MeshMGMT.UpdateLocalSpaceMousePosition(); 
    }

    private readonly ShaderProperty.FloatValue _dxProp      = new ShaderProperty.FloatValue("_dx");
    private readonly ShaderProperty.FloatValue _dyProp      = new ShaderProperty.FloatValue("_dy");
    private readonly ShaderProperty.FloatValue _sizeProp    = new ShaderProperty.FloatValue("_Size");

    private const KeyCode verticalPlanesKey = KeyCode.Z;
    private const KeyCode horisontalPlaneKey = KeyCode.X;

    public static readonly string ToolTip = "{0} {1}: Toggle vertical grid orientations {0} {2}: Set grid horizontal {0} Scroll wheel can change grid projection while in play mode {0}".F(pegi.EnvironmentNl, verticalPlanesKey, horisontalPlaneKey); 

    private void Update() {

        if (!enabled)
            return;

        if (Application.isPlaying) {

            if (Input.GetKeyDown(verticalPlanesKey))
                ScrollsProcess(1);
            else if (Input.GetKeyDown(horisontalPlaneKey))
                ScrollsProcess(-1);
            else
                ScrollsProcess(Input.GetAxis("Mouse ScrollWheel"));
        }


        if (!MeshManager.target && TexMgmtData)
            UpdatePositions();
        
    }

    private void OnEnable() =>_inst = this;
    
    public void FeedEvent(Event e) {

        if (!rendy || !rendy.enabled)
            return;

        if (e.isMouse) 
            UpdatePositions();


        if (e.type == EventType.KeyDown)  {

            bool isHorisontal = e.keyCode == verticalPlanesKey;

            if (isHorisontal || e.keyCode == horisontalPlaneKey) {
                ScrollsProcess(isHorisontal ? 1 : -1);
                UpdatePositions();
                e.Use();
            }
        }

        if (EditorInputManager.GetMouseButtonDown(2)) {
            RaycastHit hit;
            if (Physics.Raycast(EditorInputManager.GetScreenMousePositionRay(TexMGMT.MainCamera), out hit))
                onGridPos = hit.point;
        }
    }
    
}
