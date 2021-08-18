
using gf;
using System.Threading;


namespace GForce
{
    public class GForceHub
    {
        public static GForceHub instance
        {
            get { return mInstance; }
        }

        public GForceHub()
        {
        }

        ~GForceHub()
        {
            GForceLogger.Log("~GForceHub()");
            Terminate();
        }

        private static GForceHub mInstance = new GForceHub();
        private Hub mHub = null;

        private volatile bool bRunThreadRun = false;

        public string lastlog;

        private static void DebugLog(Hub.LogLevel level, string value)
        {
            mInstance.lastlog = value;

            if (level >= Hub.LogLevel.GF_LOG_ERROR)
                GForceLogger.LogError(value);
            else
                GForceLogger.Log(value);
        }

        public void Prepare()
        {
            if (bRunThreadRun && runThread != null)
            {
                return;
            }

            if (mHub != null)
            {
                mHub.Dispose();
            }

            mHub = Hub.Instance;

#if !UNITY_ANDROID
            mHub.setClientLogMethod(new Hub.logFn(GForceHub.DebugLog));
#endif
            RetCode ret;

            //RetCode ret = mHub.registerListener(mLsn);
            //GForceLogger.LogFormat("registerListener = {0}", ret);

            ret = mHub.init(0);
            GForceLogger.LogFormat("init = {0}", ret);
            GForceLogger.LogFormat("Hub status is {0}", mHub.getStatus());

            mHub.setWorkMode(Hub.WorkMode.Polling);
            GForceLogger.LogFormat("New work mode is {0}", mHub.getWorkMode());

            bRunThreadRun = true;
            runThread = new Thread(new ThreadStart(runThreadFn));
            runThread.Priority = ThreadPriority.AboveNormal;
            runThread.Start();

            //ret = mHub.startScan();
            //GForceLogger.LogFormat("startScan = {0}", ret);

            //if (RetCode.GF_SUCCESS == ret)
            //{
            //    lastlog = "BLE scan starting succeeded.";
            //}
            //else
            //{
            //    lastlog = "BLE scan starting failed.";
            //}
        }

        public void Terminate()
        {
            bRunThreadRun = false;

            if (runThread != null)
            {
                runThread.Join();
                runThread = null;
            }

            //mHub.unregisterListener(mLsn);
#if !UNITY_ANDROID
            mHub.setClientLogMethod(null);
#endif
            mHub.deinit();
            mHub.Dispose();
            mHub = null;
        }

        private Thread runThread;

        private void runThreadFn()
        {
            int loop = 0;

            while (bRunThreadRun)
            {
                RetCode ret = mHub.run(50);

                if (RetCode.GF_SUCCESS != ret && RetCode.GF_ERROR_TIMEOUT != ret)
                {
                    GForceLogger.Log("mHub.run(50) returned " + ret);
                    Thread.Sleep(5);
                    continue;
                }

                loop++;
#if DEBUG
                if (loop % 200 == 0)
                    GForceLogger.LogFormat("runThreadFn: {0} seconds elapsed.", loop * 50 / 1000);
#endif
            }

            GForceLogger.Log("Leave thread");
        }
    }
}