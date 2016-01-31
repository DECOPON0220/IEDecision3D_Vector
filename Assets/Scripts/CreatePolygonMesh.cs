using UnityEngine;
using System.Collections;
using System;

public class CreatePolygonMesh : MonoBehaviour
{
    // 変数宣言
    private Mesh m;
    private float ori_x, ori_z;
    private float bottom;
    private float inner;
    private float outer;
    private float top;
    private Vector3[] t_vertices;
    private int[] t_triangles;
    private Vector2[] t_uv;

    int num = 8;
    int y_num = 4;
    private float[] xdata;
    private float[] ydata;
    private float[] zdata;




    public void Init()
    {
        m = new Mesh();

        // 図形の座標を格納
        xdata = new float[num];
        zdata = new float[num];
        ydata = new float[y_num];

        xdata[0] = 120; zdata[0] = 180;
        xdata[1] = 120; zdata[1] = 60;
        xdata[2] = 240; zdata[2] = 60;
        xdata[3] = 240; zdata[3] = 180;

        // 内部
        xdata[4] = 240; zdata[4] = 80;
        xdata[5] = 140; zdata[5] = 80;
        xdata[6] = 140; zdata[6] = 160;
        xdata[7] = 240; zdata[7] = 160;

        ydata[0] = 70; ydata[1] = 90; ydata[2] = 150; ydata[3] = 170;

        for (int i = 0; i < num; i++)
        {
            zdata[i] = zdata[i] - 240;
        }

        // 頂点座標の指定
        Vector3[] vertices = new Vector3[]
        {
            // 底面
            new Vector3(xdata[0], ydata[0], zdata[0]),
            new Vector3(xdata[1], ydata[0], zdata[1]),
            new Vector3(xdata[2], ydata[0], zdata[2]),
            new Vector3(xdata[3], ydata[0], zdata[3]),

            // 下から二層目
            new Vector3(xdata[4], ydata[1], zdata[4]),
            new Vector3(xdata[5], ydata[1], zdata[5]),
            new Vector3(xdata[6], ydata[1], zdata[6]),
            new Vector3(xdata[7], ydata[1], zdata[7]),

            // 上から二層目
            new Vector3(xdata[4], ydata[2], zdata[4]),
            new Vector3(xdata[5], ydata[2], zdata[5]),
            new Vector3(xdata[6], ydata[2], zdata[6]),
            new Vector3(xdata[7], ydata[2], zdata[7]),

            // 上面
            new Vector3(xdata[0], ydata[3], zdata[0]),
            new Vector3(xdata[1], ydata[3], zdata[1]),
            new Vector3(xdata[2], ydata[3], zdata[2]),
            new Vector3(xdata[3], ydata[3], zdata[3])
        };
        // 三角形ごとの頂点インデックスを指定
        int[] triangles = new int[]
        {
            // 底面
            0,1,2,
            2,3,0,
            
            // 下から二層目
            4,5,7,
            7,5,6,

            // 上から二層目
            9,8,11,
            9,11,10,

            // 上面 
            12,14,13,
            14,12,15,

            // 横（外）
            0,12,13,
            0,13,1,
            0,3,15,
            0,15,12,
            1,14,2,
            1,13,14,

            // 横（内）
            4,9,5,
            4,8,9,
            5,10,6,
            5,9,10,
            6,11,7,
            6,10,11,
            
            // 横（それ以外）
            2,4,3,
            3,4,7,
            3,7,15,
            7,11,15,
            15,11,14,
            11,8,14,
            14,8,2,
            8,4,2
        };
        // UVの指定 (頂点数と同じ数を指定すること)
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.0f)
        };

        m.vertices = vertices;
        m.uv = uv;
        m.triangles = triangles;

        m.RecalculateBounds();
        m.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = m;
        GetComponent<MeshFilter>().sharedMesh.name = "myMesh";
    }

    //---------------------------------------------------------
    // 関数名 : overrideXZData
    // 機能   : 物体情報との内部外部判定結果から、図形のXYデータを書き換える
    // 引数   : x/図形のXデータ y/図形のYデータ flag/観測点の内部外部値
    // 戻り値 : なし
    //---------------------------------------------------------
    public void overrideXYData(int[,,] flag, int y)
    {
        // メッシュ情報を保存
        t_vertices = m.vertices;
        t_triangles = m.triangles;
        t_uv = m.uv;

        int m_z = GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL;
        int m_x = GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL;

        for (int t_z = 0; t_z < m_z; t_z++)
        {
            for (int t_x = 0; t_x < m_x; t_x++)
            {
                if (flag[t_x, y, t_z] == 3 &&
                    t_x != 0 && t_x != m_x &&
                    t_z != 0 && t_z != m_z)
                {
                    // 3の周辺の情報を調べる
                    // 横:Z軸+方向から
                    if (flag[t_x, y, t_z - 1] == 1)
                    {
                        // 図形が端まで達しているときは移動しない
                        for (int i = 0; i < num; i++)
                        {
                            if (zdata[i] > -20)
                            {
                                return;
                            }
                        }

                        for (int i = 0; i < num; i++)
                        {
                            zdata[i] = zdata[i] + 3;
                        }
                        for (int i = 0; i < t_vertices.Length; i++)
                        {
                            t_vertices[i].z = t_vertices[i].z + 3.0f;
                        }
                    }
                    // 横:Z軸-方向から
                    else if (flag[t_x, y, t_z + 1] == 1)
                    {
                        // 図形が端まで達しているときは移動しない
                        for (int i = 0; i < num; i++)
                        {
                            if (zdata[i] < - GlobalVar.CAMERA_WIDTH + 20)
                            {
                                return;
                            }
                        }

                        for (int i = 0; i < num; i++)
                        {
                            zdata[i] = zdata[i] - 3;
                        }
                        for (int i = 0; i < t_vertices.Length; i++)
                        {
                            t_vertices[i].z = t_vertices[i].z - 3.0f;
                        }
                    }
                    // 横:Z軸-方向から
                    if (flag[t_x - 1, y, t_z] == 1)
                    {
                        // 図形が端まで達しているときは移動しない
                        for (int i = 0; i < num; i++)
                        {
                            if (xdata[i] > GlobalVar.CAMERA_WIDTH - 20)
                            {
                                return;
                            }
                        }

                        for (int i = 0; i < num; i++)
                        {
                            xdata[i] = xdata[i] + 3;
                        }
                        for (int i = 0; i < t_vertices.Length; i++)
                        {
                            t_vertices[i].x = t_vertices[i].x + 3.0f;
                        }
                    }
                    // 横:Z軸+方向から
                    else if (flag[t_x + 1, y, t_z] == 1)
                    {
                        // 図形が端まで達しているときは移動しない
                        for (int i = 0; i < num; i++)
                        {
                            if (xdata[i] < 20)
                            {
                                return;
                            }
                        }

                        for (int i = 0; i < num; i++)
                        {
                            xdata[i] = xdata[i] - 3;
                        }
                        for (int i = 0; i < t_vertices.Length; i++)
                        {
                            t_vertices[i].x = t_vertices[i].x - 3.0f;
                        }
                    }
                }
            }
        }

        // メッシュ情報クリア
        m.Clear();

        // 変更した情報をコピー
        m.vertices = t_vertices;
        m.triangles = t_triangles;
        m.uv = t_uv;

        // 表示
        GetComponent<MeshFilter>().sharedMesh = m;
        GetComponent<MeshFilter>().sharedMesh.name = "myMesh";
    }

    public int[,,] getIODMonitoringPoint(int[,,] flag)
    {
        int m_y;

        m_y = GlobalVar.CAMERA_HEIGHT;

        for (int y = GlobalVar.POINT_INTERVAL / 2; y < m_y; y = y + GlobalVar.POINT_INTERVAL)
        {
            ArrayList m_vec = new ArrayList();
            ArrayList p_vec = new ArrayList();

            if (y > ydata[0] && y < ydata[y_num - 1])
            {
                // X,Z軸の観測点を配列に格納
                insertMonitoringDataToArray(m_vec);

                // それぞれのYの際の、ポリゴンの座標情報を配列に格納
                insertPolygonDataToArray(p_vec, y);

                // 二次元的な処理
                for (int i = 0; i < m_vec.Count; i++)
                {
                    double degree = 0;
                    Complex m_com = (Complex)m_vec[i];

                    for (int j = 0; j < p_vec.Count - 1; j++)
                    {
                        Complex f_com1 = (Complex)p_vec[j];
                        Complex f_com2 = (Complex)p_vec[j + 1];
                        double t_degree = 0;

                        // 観測座標からのベクトルを取得
                        Complex t_com1 = f_com1.sub(m_com);
                        Complex t_com2 = f_com2.sub(m_com);

                        // ２つのベクトルの成す角度を取得
                        t_degree = calc2VectorAngel(t_com1, t_com2, t_degree);

                        // 外積計算
                        Complex sub1 = f_com1.sub(m_com);
                        Complex sub2 = f_com2.sub(f_com1);
                        double cross = sub1.x * sub2.y - sub1.y * sub2.x;

                        // 符号付き角度取得
                        if (cross < 0)
                        {
                            t_degree = -t_degree;
                        }

                        degree += t_degree;
                    }

                    if (degree < 361.0 && degree > 359.0)
                    {
                        flag[i % 32, (y - 5) / 10, i / 32] = 2;
                    }
                    else {
                        flag[i % 32, (y - 5) / 10, i / 32] = 1;
                    }
                }
            }
        }

        return flag;
    }

    private double calc2VectorAngel(Complex v1, Complex v2, double degree)
    {
        double c_theta, rad_theta;
        double d1 = Math.Sqrt(Math.Pow(v1.x, 2) + Math.Pow(v1.y, 2));
        double d2 = Math.Sqrt(Math.Pow(v2.x, 2) + Math.Pow(v2.y, 2));

        // cosθを取得
        c_theta = (v1.x * v2.x + v1.y * v2.y) / (d1 * d2);

        // θを取得（ラジアン）
        rad_theta = Math.Acos(c_theta);

        // ラジアンから角度に変換
        degree = rad_theta * 180 / Math.PI;

        return degree;
    }

    //---------------------------------------------------------
    // 関数名 : insertMonitoringDataToArray
    // 機能   : 観測点の座標を配列に格納
    // 引数   : v/配列
    // 戻り値 : v/配列
    //---------------------------------------------------------
    private ArrayList insertMonitoringDataToArray(ArrayList v)
    {
        int m_x, m_z, t_x, t_z;
        m_x = GlobalVar.CAMERA_WIDTH / GlobalVar.POINT_INTERVAL;
        m_z = GlobalVar.CAMERA_HEIGHT / GlobalVar.POINT_INTERVAL;

        for (int z = 0; z < m_z; z++)
        {
            for (int x = 0; x < m_x; x++)
            {
                t_x = x * GlobalVar.POINT_INTERVAL + (GlobalVar.POINT_INTERVAL / 2);
                t_z = z * GlobalVar.POINT_INTERVAL + (GlobalVar.POINT_INTERVAL / 2);

                v.Add(new Complex(t_x, t_z));
            }
        }

        return v;
    }

    //---------------------------------------------------------
    // 関数名 : insertPolygonDataToArray
    // 機能   : 
    // 引数   : 
    // 戻り値 : v/配列
    //---------------------------------------------------------
    private ArrayList insertPolygonDataToArray(ArrayList v, int y)
    {
        // ydata[0] = 70; ydata[1] = 90; ydata[2] = 150; ydata[3] = 170;
        // 第一層
        if (y > ydata[0] && y < ydata[1])
        {
            for (int i = 0; i < 4; i++)
            {
                v.Add(new Complex(xdata[i], 240 + zdata[i]));
            }

            v.Add(new Complex(xdata[0], 240 + zdata[0]));
        }
        // 中間
        else if (y >= ydata[1] && y <= ydata[2])
        {
            for (int i = 0; i < 3; i++)
            {
                v.Add(new Complex(xdata[i], 240 + zdata[i]));
            }

            for (int i = 4; i < 8; i++)
            {
                v.Add(new Complex(xdata[i], 240 + zdata[i]));
            }

            for (int i = 3; i < 4; i++)
            {
                v.Add(new Complex(xdata[i], 240 + zdata[i]));
            }

            v.Add(new Complex(xdata[0], 240 + zdata[0]));
        }

        // 第三層
        if (y > ydata[2] && y < ydata[3])
        {
            for (int i = 0; i < 4; i++)
            {
                // 配列に格納
                v.Add(new Complex(xdata[i], 240 + zdata[i]));
            }

            v.Add(new Complex(xdata[0], 240 + zdata[0]));
        }

        return v;
    }

    //---------------------------------------------------------
    // 関数名 : isExsistPolygon
    // 機能   : 
    // 引数   : y/
    // 戻り値 : 
    //---------------------------------------------------------   
    public bool isExsistPolygon(float y)
    {
        if (ydata[0] < y && ydata[y_num - 1] > y)
        {
            return true;
        }

        return false;
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
}