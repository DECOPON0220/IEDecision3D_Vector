using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using UnityEngine;
using System.Collections;



public class Main : MonoBehaviour
{
    // 変数宣言
    IplImage i_img1, i_img2;        // IplImage
    int[,] hps_arr, vps_arr;        // 縦横それぞれの点情報を格納（hps_arr:水平方向 vps_arr:垂直方向）
    int[,,] ps_arr3D;               // 3次元情報を格納（ps_arr3D:点情報）
    double[,] pl_arrXZ;             // ポリゴン座標（水平方向）
    double[] pl_arrY;               // ポリゴン座標（垂直方向）
    int[,,] io_flag;                // 観測点データ
    CreatePolygonMesh polygon;      // CreatePolygonMesh クラス参照用
    GameObject refObj;              // 3Dポリゴン取得
    public GameObject h_dot;        // 点情報表示用ポリゴン
    public GameObject i_dot;        
    float[] xdata, ydata;

    ArrayList pl_vec = new ArrayList();     // ポリゴン座標格納
    IplImage h_img1 = new IplImage();       // HSV画像
    IplImage h_img2 = new IplImage();
    Camera cam1 = new Camera();             // カメラ画像取得
    Camera cam2 = new Camera();
    Graphic g = new Graphic();              // 画像関連用クラス
    


    /*     デバッグ用（FPS）     */
    int   frameCount;
    float prevTime;
    /* ------------------------- */



    // Use this for initialization
    void Start()
    {
        // 変数宣言
        int x_window = GlobalVar.CAMERA_WIDTH;
        int y_window = GlobalVar.CAMERA_HEIGHT;

        // カメラデバイス選択、設定
        cam1.setDevice(0);  // 水平
        cam2.setDevice(1);  // 垂直

        // HSV画像初期化
        CvSize WINDOW_SIZE = new CvSize(x_window, y_window);
        h_img1 = Cv.CreateImage(WINDOW_SIZE, BitDepth.U8, 3);
        h_img2 = Cv.CreateImage(WINDOW_SIZE, BitDepth.U8, 3);

        // データ格納用配列の初期化
        int x = x_window / GlobalVar.POINT_INTERVAL;
        int y = y_window / GlobalVar.POINT_INTERVAL;
        int z = y_window / GlobalVar.POINT_INTERVAL;
        hps_arr  = new int[y, x];
        vps_arr  = new int[z, x];
        ps_arr3D = new int[x, y, z];
        pl_arrXZ = new double[GlobalVar.VERTICE_NUM / 2, 2];
        pl_arrY  = new double[2];
        io_flag  = new int[x, y, z];    // 外部（遠）:0 外部（近）:1 内部:2

        // 3Dポリゴン指定
        refObj = GameObject.Find("Object");
        polygon = refObj.GetComponent<CreatePolygonMesh>();
        polygon.Init();

        // 観測点データ初期化
        init3DArr(ps_arr3D);
        initMFlag(io_flag);

        // 図形と観測点の内部外部判定を行う
        polygon.getIODMonitoringPoint(io_flag);
        
        /*     デバッグ用（FPS）     */
        frameCount = 0;
        prevTime = 0.0f;
        /* ------------------------- */
    }

    // Update is called once per frame
    void Update()
    {
        /*     デバッグ用（FPS）     */
        frameCount++;
        float time = Time.realtimeSinceStartup - prevTime;
        /* ------------------------- */

        // カメラ画像の取得
        i_img1 = cam1.getCameraImage();
        i_img2 = cam2.getCameraImage();

        // カメラ画像をBGRからHSVへ変換
        g.convertBgrToHsv(i_img1, h_img1);
        g.convertBgrToHsv(i_img2, h_img2);

        // 平滑化
        g.convertSmooothing(h_img1);
        g.convertSmooothing(h_img2);

        // カメラ画像の任意の点からデータ取得
        g.getPointData(h_img1, hps_arr);
        g.getPointData(h_img2, vps_arr);

        // 縦横の点情報を結合して3次元配列に格納
        bondPosStaArr(vps_arr, hps_arr, ps_arr3D);

        // 情報が存在するレイヤー(Y軸方向)において、内部外部判定を行う
        for (int y = 0; y < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; y++)
        {
            // 情報が存在する場合
            if (isExsistInfo3DArrY(ps_arr3D, y))
            {
                // 同じ階層にポリゴンがある場合
                if (polygon.isExsistPolygon(y * GlobalVar.POINT_INTERVAL))
                {
                    //Debug.Log("同じ階層にポリゴンあり");
                    if (isInsideOrOutside(io_flag, ps_arr3D, y))
                    {
                        //Debug.Log("内部に手あり");
                        polygon.overrideXYData(io_flag, y);

                        // 観測点データを初期化
                        init3DArr(ps_arr3D);
                        initMFlag(io_flag);

                        // 図形と観測点の内部外部判定を行う
                        polygon.getIODMonitoringPoint(io_flag);

                        break;
                    }
                }
            }
        }

        displayIDot(io_flag);

        // 物体を3D表示
        display3Ddot(ps_arr3D);

        /*     デバッグ用（FPS）     */
        if (time >= 0.5f)
        {
            //Debug.LogFormat("{0}fps", frameCount/time);
            frameCount = 0;
            prevTime = Time.realtimeSinceStartup;
        }
        /* ------------------------- */
    }

    //---------------------------------------------------------
    // 関数名 : isInsideOrOutside
    // 機能   : 物体情報が内部にあるかチェック
    // 引数   : flag/観測点の内部外部値 arr/物体の情報
    // 戻り値 : true/内部あり false/内部なし
    //---------------------------------------------------------
    private bool isInsideOrOutside(int[,,] flag, int[,,] arr, int y)
    {
        int m_x = GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL;
        int m_z = GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL;

        for (int z = 0; z < m_z; z++)
        {
            for (int x = 0; x < m_x; x++)
            {
                if (flag[x, y, z] == 2 && arr[x, y, z] == 2)
                {
                    flag[x, y, z] = 3;
                    return true;
                }
            }
        }

        return false;
    }


    //---------------------------------------------------------
    // 関数名 : displayIDot
    // 機能   : 情報が格納されている配列から3Dに起こす
    // 引数   : arr/情報
    // 戻り値 : なし
    //---------------------------------------------------------
    private void displayIDot(int[,,] flag)
    {
        // 変数宣言
        float i_x = 0;
        float i_y = 0;
        float i_z = -240;
        float fps = 0.05F;
        float t_x, t_y, t_z;

        for (int y = 0; y < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; y++)
        {
            for (int x = 0; x < GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL; x++)
            {
                for (int z = 0; z < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; z++)
                {
                    if (flag[x, y, z] == 2)
                    {
                        t_x = i_x;
                        t_y = i_y;
                        t_z = i_z;

                        t_x = t_x + (GlobalVar.POINT_INTERVAL / 2) + (GlobalVar.POINT_INTERVAL * x);
                        t_y = t_y + (GlobalVar.POINT_INTERVAL / 2) + (GlobalVar.POINT_INTERVAL * y);
                        t_z = t_z + (GlobalVar.POINT_INTERVAL / 2) + (GlobalVar.POINT_INTERVAL * z);

                        GameObject dots = Instantiate(i_dot, new Vector3(t_x, t_y, t_z), transform.rotation) as GameObject;
                        Destroy(dots, fps);
                    }
                }
            }
        }
    }

    //---------------------------------------------------------
    // 関数名 : initMFlag
    // 機能   : 観測点からのデータを初期化する
    // 引数   : flag/観測点の内部外部値
    // 戻り値 : flag/観測点の内部外部値
    //---------------------------------------------------------
    private int[,,] initMFlag(int[,,] flag)
    {
        int m_x = GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL;
        int m_y = GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL;
        int m_z = GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL;

        for (int x = 0; x < m_x; x++)
        {
            for (int y = 0; y < m_y; y++)
            {
                for (int z = 0; z < m_z; z++)
                {
                    flag[x, y, z] = 0;
                }
            }
        }

        return flag;
    }

    //---------------------------------------------------------
    // 関数名 : isExsistInfo3DArrY
    // 機能   : 三次元配列について、指定されたYの時、情報が格納されているか確認する
    // 引数   : xyz_arr/ y/
    // 戻り値 : true/ false/
    //---------------------------------------------------------
    private bool isExsistInfo3DArrY(int[,,] xyz_arr, int y)
    {
        for (int x = 0; x < GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL; x++)
        {
            for (int z = 0; z < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; z++)
            {
                if (xyz_arr[x, y, z] == 2) {
                    return true;
                }
            }
        }

        return false;
    }

    //---------------------------------------------------------
    // 関数名 : bondPosStaArr
    // 機能   : 青以外の情報が格納されている数を取得
    // 引数   : x_arr/X情報 y_arr/Y情報
    // 戻り値 : xyz_arr/
    //---------------------------------------------------------
    private int[,,] bondPosStaArr(int[,] xz_arr, int[,] xy_arr, int[,,] xyz_arr)
    {
        // 配列を初期化
        init3DArr(xyz_arr);

        // 下から情報があるか確認している
        for (int y = 0; y < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; y++)
        {
            for (int x = 0; x < GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL; x++)
            {
                // 情報を発見したとき
                if (xy_arr[(GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL) - 1 - y, x] == 1) {
                    for (int z = 0; z < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; z++)
                    {
                        if (xz_arr[z, x] == 1)
                        {
                            xyz_arr[x, y, (GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL) - 1 - z] = 2;
                        }
                        else
                        {
                            xyz_arr[x, y, (GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL) - 1 - z] = 0;
                        }
                    }
                }
            }
        }

        return xyz_arr;
    }

    //---------------------------------------------------------
    // 関数名 : delArrNoise
    // 機能   : 情報が格納されている配列からノイズ情報を除去する
    // 引数   : arr/情報
    // 戻り値 : なし
    //---------------------------------------------------------
    private int[,,] delArrNoise(int[,,] arr)
    {
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              

        return arr;
    }

    //---------------------------------------------------------
    // 関数名 : display3Ddot
    // 機能   : 情報が格納されている配列から3Dに起こす
    // 引数   : arr/情報
    // 戻り値 : なし
    //---------------------------------------------------------
    private void display3Ddot(int[,,] arr)
    {
        // 変数宣言
        float i_x = 0;
        float i_y = 0;
        float i_z = -240;
        float fps = 0.05F;
        float t_x, t_y, t_z;

        for (int y = 0; y < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; y++)
        {
            for (int x = 0; x < GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL; x++)
            {
                for (int z = 0; z < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; z++)
                {
                    if (arr[x,y,z] == 2)
                    {
                        t_x = i_x;
                        t_y = i_y;
                        t_z = i_z;

                        t_x = t_x + (GlobalVar.POINT_INTERVAL / 2) + (GlobalVar.POINT_INTERVAL * x);
                        t_y = t_y + (GlobalVar.POINT_INTERVAL / 2) + (GlobalVar.POINT_INTERVAL * y);
                        t_z = t_z + (GlobalVar.POINT_INTERVAL / 2) + (GlobalVar.POINT_INTERVAL * z);

                        GameObject dots = Instantiate(h_dot, new Vector3(t_x, t_y, t_z), transform.rotation) as GameObject;
                        Destroy(dots, fps);
                    }
                }
            }
        }
    }


    //---------------------------------------------------------
    // 関数名 : init3DArr
    // 機能   : 情報が格納されている配列から3Dに起こす
    // 引数   : arr/情報
    // 戻り値 : なし
    //---------------------------------------------------------
    private void init3DArr(int[,,] arr)
    {
        for (int y = 0; y < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; y++)
        {
            for (int x = 0; x < GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL; x++)
            {
                for (int z = 0; z < GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL; z++)
                {
                    arr[x, y, z] = 0;
                }
            }
        }
    }

    //---------------------------------------------------------
    // 関数名 : init3DArr2
    // 機能   : 情報が格納されている配列から3Dに起こす
    // 引数   : arr/情報
    // 戻り値 : なし
    //---------------------------------------------------------
    private void init3DArr2(int[,,] arr)
    {
        for (int y = 0; y < (GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL) + 2; y++)
        {
            for (int x = 0; x < (GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL) + 2; x++)
            {
                for (int z = 0; z < (GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL) + 2; z++)
                {
                    arr[x, y, z] = 0;
                }
            }
        }
    }

        /// <summary>
        /// 終了処理
        /// </summary>
        void OnApplicationQuit()
    {
        // リソースの破棄
        Cv.ReleaseImage(i_img1);
        Cv.ReleaseImage(i_img2);
        Cv.ReleaseImage(h_img1);
        Cv.ReleaseImage(h_img2);
        cam1.Release();
        cam2.Release();
    }
}

class Complex
{
    public double x;
    public double y;
    bool flag;

    public Complex(double x, double y)
    {
        this.x = x;
        this.y = y;
        this.flag = false;
    }

    Complex add(Complex c2)
    {//複素数足し算
        return new Complex(this.x + c2.x, this.y + c2.y);
    }
    public Complex sub(Complex c2)
    {//複素数引き算
        return new Complex(this.x - c2.x, this.y - c2.y);
    }
    Complex mul(Complex c2)
    {//複素数掛け算
        return new Complex(this.x * c2.x - this.y * c2.y, this.x * c2.y + this.y * c2.x);
    }
    Complex div(Complex c2)
    {//複素数割り算
        double d = (c2.x * c2.x) + (c2.y * c2.y);
        return new Complex(((this.x * c2.x) + (this.y * c2.y)) / d, (-(this.x * c2.y) + (this.y * c2.x)) / d);
    }

    //void print()
    //{
    //    System.out.println(this.x + "+" + this.y + "*i");
    //}

    /*boolean caushy(Complex c1,Complex c2){
	}*/
    double realPart()
    {
        return this.x;
    }
    double realAbs()
    {
        if (this.x >= 0)
        {
            return this.x;
        }
        return this.x * (-1);
    }
    double imaginaryPart()
    {
        return this.y;
    }
    double imaginaryAbs()
    {
        if (this.x >= 0) { return this.y; }
        return this.y * (-1);
    }
}