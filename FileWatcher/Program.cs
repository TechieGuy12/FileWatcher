using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.Notifications;

namespace TE.FileWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Watches watches;
            Notifications.Notifications notifications;

            XmlSerializer serializer = new XmlSerializer(typeof(Watches));
            using (FileStream fs = new FileStream(@"C:\Temp\config.xml", FileMode.Open))
            {
                watches = (Watches)serializer.Deserialize(fs);
            }

            serializer = new XmlSerializer(typeof(Notifications.Notifications));
            using (FileStream notifyfs = new FileStream(@"C:\Temp\notification.xml", FileMode.Open))
            {
                notifications = (Notifications.Notifications)serializer.Deserialize(notifyfs);
            }

            Task[] tasks = new Task[watches.WatchList.Count];
            int count = 0;

            foreach (Watch watch in watches.WatchList)
            {
                try
                {
                    tasks[count] = Task.Run(() => { Watcher watcher = new Watcher(watch, notifications); });
                    count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ae)
            {
                foreach (Exception exception in ae.Flatten().InnerExceptions)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }
    }
}
