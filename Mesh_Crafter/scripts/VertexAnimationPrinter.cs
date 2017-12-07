using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshEditingTools;

public class vertAnimNo {
    static int inTextureInd = 1;
    public vertexAnimationFrame frame;
    public int inTextureIndex;

    public vertAnimNo() {
        inTextureIndex = inTextureInd;
        inTextureInd++;
    }
}

public class orderedLine<T> where T: new() {

   public class orderTicketsbase {
        public orderTicketsbase previous;
        public orderTicketsbase next;
        public int index;
        public T val;

        public orderTicketsbase() {
            val = new T();
        }
    }

    int size;

    orderTicketsbase[] line;
    orderTicketsbase head;
    orderTicketsbase tail;

    public orderedLine (int nsize){
        size = nsize;
        line = new orderTicketsbase[size];

        for (int i = 0; i < size; i++)
            line[i] = new orderTicketsbase();

        line[0].next = line[1];
        int cnt = size - 1;
        for (int i = 1; i < cnt; i++) {
            orderTicketsbase otb = line[i];
            otb.previous = line[i - 1];
            otb.next = line[i + 1];
        }

        line[cnt].previous = line[cnt - 1];

        tail = line[0];
        head = line[cnt];
    }

    public T useTail() {
        use(tail);

        return head.val;

    }
  
    void use(orderTicketsbase otb) {
        if (otb == head) return;

        otb.next.previous = otb.previous;
        if (otb != tail)
            otb.previous.next = otb.next;
        else tail = otb.next;

        head.next = otb;
        otb.previous = head;

        head = otb;
    }
}


public class VertexAnimationPrinter : MonoBehaviour {

    public static VertexAnimationPrinter inst;
    public RenderTexture VertexAnimationTexture;
    public int size = 128;
    public Camera cam;

    orderedLine<vertAnimNo> animTexLines;

    public int UpdateLineFor (vertexAnimationFrame frame) {
        vertAnimNo tmp = frame.animTexLines;
        int UVy = tmp.inTextureIndex;
        PixelPrinter.inst().RenderSector(0, UVy, size, 1, Vector4.zero);

        cam.Render();
        PixelPrinter.inst().Clean();
            List<int> inds;
        List<Vector3> poses = frame.verts.GetAllObjs(out inds);

        for (int i = 0; i < poses.Count; i++)
            PixelPrinter.inst().RenderPixel(inds[i], UVy, poses[i]);

        cam.Render();
        PixelPrinter.inst().Clean();
        return UVy;
    }

    public int GetNewLineFor (vertexAnimationFrame frame) {
        vertAnimNo tmp = animTexLines.useTail();
        frame.animTexLines = tmp;
        tmp.frame = frame;
        return UpdateLineFor(frame);
    }

    private void Start() {
        VertexAnimationTexture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        VertexAnimationTexture.filterMode = FilterMode.Point;
        Shader.SetGlobalTexture("_vertAnim", VertexAnimationTexture);
        animTexLines = new orderedLine<vertAnimNo>(size - 1);
    }

    

    void Awake () {
        inst = this;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
