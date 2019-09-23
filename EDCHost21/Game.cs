﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace EDC21HOST
{
    public enum GameState { Unstart = 0, Normal = 1, Pause = 2, End = 3 };
    public class Game
    {
        public bool DebugMode; //调试模式，最大回合数 = 1,000,000
        public const int MaxSize = 270;
        public const int MaxPersonNum = 4;
        public const int MazeCrossNum = 6;
        public const int MazeCrossDist = 30;
        public const int MazeBorderPoint1 = 37;
        public const int MazeBorderPoint2 = MazeBorderPoint1 + MazeCrossDist * (1 + MazeCrossNum);
        public const int MaxCarryDistance = 10; //接上人员的最大距离
        public const int MaxCarBallDistance = 30; //拿到小球的最大距离
        public const int MinBallSept = 6; //小球最小可分辨距离
        public const int CollectBound = 36;
        public const int StorageBound = 43;
        public const int PersonGetScore = 15;
        public const int BallGetScore = 10;
        public const int BallOwnScore = 10;
        public const int BallOppoScore = 5;

        public int APauseNum = 0;
        public int BPauseNum = 0;
        public int AFoul1 = 0;
        public int AFoul2 = 0;
        public int BFoul1 = 0;
        public int BFoul2 = 0;
        public int MaxRound;  //最大回合数
        public int GameCount; //上下半场、加时等
        public int Round { get; set; }//当前回合
        public GameState State { get; set; }
        public Car CarA, CarB;
        public Person[] People;
        public bool RequestNewBall; //当前是否需要新球
        public Camp CollectCamp; //物资收集点处为A车或B车
        public List<Dot> BallsDot; //小球位置
        public Dot BallAtCollect; //物资收集点处的小球位置
        public int BallCntA, BallCntB; //物资存放点处小球个数
        public PersonGenerator Generator { get; set; }
        public int CurrPersonNumber; //当前人员数量
        //public static bool[,] GameMap = new bool[MaxSize, MaxSize]; //地图信息
        public FileStream FoulTimeFS;
        public static bool InMaze(Dot dot)
        {
            if (InRegion((i, j) => (i >= MazeBorderPoint1 && i <= MazeBorderPoint2 && j >= MazeBorderPoint1 && j <= MazeBorderPoint2), dot))
                return true;
            else return false;
        }
        public static bool InCollect(Dot dot)
        {
            if (InRegion((i, j) => (i <= CollectBound && j <= CollectBound), dot))
                return true;
            else return false;
        }
        public static bool InStorageA(Dot dot)
        {
            if (InRegion((i, j) => (i <= StorageBound && j >= MaxSize - StorageBound), dot))
                return true;
            else return false;
        }
        public static bool InStorageB(Dot dot)
        {
            if (InRegion((i, j) => (j <= StorageBound && i >= MaxSize - StorageBound), dot))
                return true;
            else return false;
        }

        private static bool InRegion(Func<int, int, bool> inRegion, Dot dot)//点是否有效->3*3方格在区域内的格数大于4
        {
            int count = 0;
            for (int i = ((dot.x - 1 > 0) ? (dot.x - 1) : 0); i <= ((dot.x + 1 < MaxSize) ? (dot.x + 1) : MaxSize - 1); ++i)
                for (int j = ((dot.y - 1 > 0) ? (dot.y - 1) : 0); j <= ((dot.y + 1 < MaxSize) ? (dot.y + 1) : MaxSize - 1); ++j)
                    if (inRegion(i, j))
                        count++;
            if (count >= 4) return true;
            else return false;
        }

        //public static void LoadMap()//读取地图文件
        //{
        //    //FileStream MapFile = File.OpenRead("../../map/map.bmp");
        //    //byte[] buffer = new byte[MapFile.Length - 54]; //存储图片文件
        //    //MapFile.Position = 54;
        //    //MapFile.Read(buffer, 0, buffer.Length);
        //    //for (int i = 0; i != MaxSize; ++i)
        //    //    for (int j = 0; j != MaxSize; ++j)
        //    //        if (buffer[(i * MaxSize + j) * 3 + 2 * i] > 128)//白色
        //    //            GameMap[j, i] = true;
        //    //        else
        //    //            GameMap[j, i] = false;

        //    Bitmap mapData = new Bitmap("../../map/map.bmp");
        //    for (int i = 0; i != MaxSize; ++i)
        //        for (int j = 0; j != MaxSize; ++j)
        //            GameMap[j, i] = mapData.GetPixel(j, i).Equals(Color.FromArgb(255, 255, 255));

        //    //using (StreamWriter sw = new StreamWriter("../../map/map.txt"))
        //    //{
        //    //    for (int i = 0; i != MaxSize; ++i)
        //    //    {
        //    //        for (int j = 0; j != MaxSize; ++j)
        //    //            sw.Write((GameMap[j, i] = mapData.GetPixel(j, i).Equals(Color.FromArgb(255, 255, 255))) ? '1' : '0');
        //    //        sw.Write('\n');
        //    //    }
        //    //}
        //}
        public static Dot OppoDots(Dot prevDot)
        {
            Dot newDots = new Dot();
            newDots.x = prevDot.y;
            newDots.y = prevDot.x;
            return newDots;
        }
        public static double GetDistance(Dot A, Dot B)
        {
            return Math.Sqrt((A.x - B.x) * (A.x - B.x) + (A.y - B.y) * (A.y - B.y));
        }
        public Game()
        {
            GameCount = 1;
            MaxRound = 1200;
            BallsDot = new List<Dot>();
            BallAtCollect = new Dot(0, 0);
            RequestNewBall = false;
            BallCntA = BallCntB = 0;
            CollectCamp = Camp.None;
            CarA = new Car(Camp.CampA);
            CarB = new Car(Camp.CampB);
            People = new Person[MaxPersonNum];
            Round = 0;
            State = GameState.Unstart;
            InitialPerson();
            DebugMode = false;
            FoulTimeFS = null;
        }

        public void NextStage()
        {
            ++GameCount;
            if (GameCount >= 3)
                MaxRound = 600;
            else
                MaxRound = 1200;
            Round = 0;
            State = GameState.Unstart;
            InitialPerson();
            DebugMode = false;
            if (FoulTimeFS != null)
            {
                byte[] data = Encoding.Default.GetBytes($"nextStage\r\n");
                FoulTimeFS.Write(data, 0, data.Length);
            }
        }

        protected void InitialPerson()//初始化人员
        {
            Generator = new PersonGenerator();
            Generator.Generate(100);
            for (int i = 0; i < MaxPersonNum; ++i)
                People[i] = new Person();
            for (int i = 0; i < MaxPersonNum; ++i)
                People[i] = new Person(Generator.Next(People), i);
            CheckPersonNumber();
        }
        protected void CheckPersonNumber() //根据回合数更改最大人员数量
        {
            CurrPersonNumber = MaxPersonNum;
        }
        public void NewPerson(Dot currentPersonDot, int num) //刷新这一位置的新人员
        {
            Dot temp = new Dot();
            do
            {
                temp = Generator.Next(People);
            }
            while (temp == currentPersonDot); //防止与刚接上人员位置相同
            People[num] = new Person(temp, num);
        }

        //增加分数
        public void AddScore(Camp c, int score)
        {
            switch (c)
            {
                case Camp.CampA:
                    CarA.Score += score;
                    if (CarA.Score < 0) CarA.Score = 0;
                    return;
                case Camp.CampB:
                    CarB.Score += score;
                    if (CarB.Score < 0) CarB.Score = 0;
                    return;
                default: return;
            }
        }

        public void Start() //开始比赛
        {
            State = GameState.Normal;
            CarA.Start();
            CarB.Start();
        }
        public void Pause() //暂停比赛
        {
            State = GameState.Pause;
            CarA.Stop();
            CarB.Stop();
        }
        public void End() //结束比赛
        {
            State = GameState.End;
        }
        //复位
        public void AskPause(Camp c)
        {
            Pause();
            Round -= 50;
            if (Round < 0) Round = 0;
            switch (c)
            {
                case Camp.CampA:
                    ++APauseNum;
                    break;
                case Camp.CampB:
                    ++BPauseNum;
                    break;
            }
        }

        //更新小球相关操作的状态、得分
        public void UpdateBallsState()
        {
            bool noBallInCollect = true;
            int currBallCntA = 0, currBallCntB = 0;
            BallAtCollect = new Dot(0, 0);
            foreach (Dot ball in BallsDot)
            {
                if (InCollect(ball))
                {
                    BallAtCollect = ball;
                    noBallInCollect = false;
                }
                else if (InStorageA(ball)) currBallCntA++;
                else if (InStorageB(ball)) currBallCntB++;
            }

            //更新CollectCamp：物资收集点处是A车还是B车
            if (InCollect(CarA.Pos) && InCollect(CarB.Pos))
            {
                if (GetDistance(CarA.Pos, BallAtCollect) < GetDistance(CarB.Pos, BallAtCollect)) //若物资收集点处没有球，则BallAtCollect为(0, 0)
                    CollectCamp = Camp.CampA;
                else
                    CollectCamp = Camp.CampB;
            }
            else if (InCollect(CarA.Pos))
                CollectCamp = Camp.CampA;
            else if (InCollect(CarB.Pos))
                CollectCamp = Camp.CampB;
            //else：不更新

            RequestNewBall = noBallInCollect && !InCollect(CarA.Pos) && !InCollect(CarB.Pos); //物资收集点处没有车和球时才可请求新球
            
            //抓取到小球计分
            if (RequestNewBall) 
            {
                switch (CollectCamp)
                {
                    case Camp.CampA: AddScore(Camp.CampA, BallGetScore); CollectCamp = Camp.None; break;
                    case Camp.CampB: AddScore(Camp.CampB, BallGetScore); CollectCamp = Camp.None; break;
                    default: break; 
                }
            }

            //小球运输至存放点计分
            if (currBallCntA == BallCntA + 1)
            {
                BallCntA++;
                if (GetDistance(CarA.Pos, new Dot(0, MaxSize)) < GetDistance(CarB.Pos, new Dot(0, MaxSize)))
                    AddScore(Camp.CampA, BallOwnScore);
                else
                    AddScore(Camp.CampB, BallOppoScore);
            }

            if (currBallCntB == BallCntB + 1)
            {
                BallCntB++;
                if (GetDistance(CarA.Pos, new Dot(MaxSize, 0)) < GetDistance(CarB.Pos, new Dot(MaxSize, 0)))
                    AddScore(Camp.CampA, BallOppoScore);
                else
                    AddScore(Camp.CampB, BallOwnScore);
            }
        }
        public void Update()//每回合执行
        {
            if (State == GameState.Normal)
            {
                Round++;
                //GetInfoFromCameraAndUpdate();
                CheckPersonNumber();
                UpdateBallsState();
                #region PunishmentPhase
                //if (!CarDotValid(CarA.Pos)) CarA.Stop();
                //if (!CarDotValid(CarB.Pos)) CarB.Stop();
                #endregion

                //人员上车
                for (int i = 0; i != CurrPersonNumber; ++i)
                {
                    Person p = People[i];
                    if (CarA.UnderStop == false && GetDistance(p.StartPos, CarA.Pos) < MaxCarryDistance)
                    {
                        AddScore(Camp.CampA, PersonGetScore);
                        CarA.PersonCnt++;
                        NewPerson(p.StartPos, i);
                    }

                    if (CarB.UnderStop == false && GetDistance(p.StartPos, CarB.Pos) < MaxCarryDistance)
                    {
                        AddScore(Camp.CampB, PersonGetScore);
                        CarB.PersonCnt++;
                        NewPerson(p.StartPos, i);
                    }
                }

                if ((Round >= MaxRound && DebugMode == false) || (Round >= 1000000 && DebugMode == true)) //结束比赛
                {
                    End();
                }
            }
            //byte[] message = PackMessage();
            //SendMessage
        } 
        public byte[] PackMessage()
        {
            byte[] message = new byte[56]; //上位机传递的信息
            message[0] = (byte)(Round >> 8);
            message[1] = (byte)Round;
            message[2] = (byte)(((byte)State << 6) | ((byte)(InMaze(CarA.Pos) ? 1 : 0) << 5) | ((byte)(InMaze(CarB.Pos) ? 1 : 0) << 4)
                | (CarA.Pos.x >> 5 & 0x08) | (CarA.Pos.y >> 6 & 0x04) | (CarB.Pos.x >> 7 & 0x02) | (CarB.Pos.y >> 8 & 0x01));
            for (int i = 0; i < MaxPersonNum; ++i)
            {
                message[3] |= (byte)((People[i].StartPos.x & 0x100) >> (2 * i + 1));
                message[3] |= (byte)((People[i].StartPos.y & 0x100) >> (2 * i + 2));
            }
            message[4] = InMaze(CarA.Pos) ? (byte)CarA.Pos.x : (byte)0;
            message[5] = InMaze(CarA.Pos) ? (byte)CarA.Pos.y : (byte)0;
            message[6] = InMaze(CarB.Pos) ? (byte)CarB.Pos.x : (byte)0;
            message[7] = InMaze(CarB.Pos) ? (byte)CarB.Pos.y : (byte)0;
            for (int i = 0; i < MaxPersonNum; ++i)
            {
                message[8 + i * 2] = (byte)People[i].StartPos.x;
                message[9 + i * 2] = (byte)People[i].StartPos.y;
            }
            message[16] = (byte)BallAtCollect.x;
            message[17] = (byte)BallAtCollect.y;
            message[18] = (byte)(CarA.Score >> 8);
            message[19] = (byte)CarA.Score;
            message[20] = (byte)(CarB.Score >> 8);
            message[21] = (byte)CarB.Score;
            message[22] = (byte)CarA.PersonCnt;
            message[23] = (byte)CarB.PersonCnt;
            message[24] = (byte)CarA.BallGetCnt;
            message[25] = (byte)CarB.BallGetCnt;
            message[26] = (byte)CarA.BallAtOwnCnt;
            message[27] = (byte)CarA.BallAtOppoCnt;
            message[28] = (byte)CarB.BallAtOwnCnt;
            message[29] = (byte)CarB.BallAtOppoCnt;
            message[54] = 0x0D;
            message[55] = 0x0A;
            return message;
        }
    }
}