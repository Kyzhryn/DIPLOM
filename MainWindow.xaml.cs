using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;


namespace DIPLOM
{

    public partial class MainWindow : Window
    {
        //size of data in i request
        private List<int> sizeDataInTasks;


        //deadlines of each 
        private List<int> deadlinesOfTasks; 


        //time of i request processing on the j device
        private List<List<int>> tasksOnDevices;


        //расписание загрузки на приборы: ключ - номер запроса,
        //в массиве
        //[
        //номер прибора,
        //порядковый номер на приборе,
        //время начала обработки,
        //время окончания обработки
        //]
        public Dictionary<int, int[]> sheduleOfProcessing;

        //расписание загрузки данных для запросов в хранилища
        //ключ - номер запроса, в масссиве 
        //[
        //номер хранилища,
        //номер в загрузке на данном хранилище
        //момент начала загрузки
        //]
        public Dictionary<int, int[]> sheduleOfLoading;

        //настройки по умолчанию для храрнилищ
        public static string defaultSettings = "5 500";

        //имя файла для хранения настроек
        public static string fileNameWithSettings = "settings.txt";
        public MainWindow()
        {
            int[] massiveWithSettings;
            string readedSettings = "";
            //check exists file
            if (!File.Exists(fileNameWithSettings)) //if not exist
            {
                //write and create file
                StreamWriter writerSettings = new StreamWriter(fileNameWithSettings);
                writerSettings.WriteLine(defaultSettings);
                writerSettings.Close();

                //split defaultSettings string and get countStorage and sizeStorage
                massiveWithSettings = defaultSettings
                                      .Split(' ')
                                      .Select
                                      (
                                        x => int.Parse(x)
                                      )
                                      .ToArray();
            }
            else
            {
                //open file
                StreamReader fileReadSettings = new StreamReader(fileNameWithSettings);
                if (fileReadSettings == null) //if can't open file
                {
                    MessageBox.Show("Не удалось открыть файл!");
                    return;
                }

                readedSettings = fileReadSettings.ReadLine();
                if (readedSettings == null) //if file empty
                {
                    MessageBox.Show("Файл пуст");
                    return;
                }
                //split readedSettings string and get countStorage and sizeStorage
                massiveWithSettings = readedSettings.Split(' ')
                                                            .Select(x => int.Parse(x)).ToArray();
                fileReadSettings.Close();
            }
           
            App.Current.Properties["countStorage"] = massiveWithSettings[0];
            App.Current.Properties["sizeStorage"] = massiveWithSettings[1];

            tasksOnDevices       = new List<List<int>>();
            sizeDataInTasks      = new List<int>();
            sheduleOfProcessing  = new Dictionary<int, int[]>();
            sheduleOfLoading     = new Dictionary<int, int[]>();
            deadlinesOfTasks     = new List<int>();
            InitializeComponent();
            
        }

        public void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            //windows dialog to the select some file
            OpenFileDialog openFile = new OpenFileDialog();

            //only txt files
            openFile.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            //openFileDialog.FileName - full path

            if (openFile.ShowDialog() == true) 
            {
                string allReaded = ""; //readed string
               
                StreamReader fileWithData =  new StreamReader(openFile.FileName);

                if (fileWithData == null) //if file don't exist, return
                {
                    MessageBox.Show("Не удалось открыть файл");
                    return; 
                }

                //read data from file
                //one line in file it's 
                // int[] №i task, size data in task, deadline
                while (fileWithData.EndOfStream != true)
                {
                    allReaded = fileWithData.ReadLine();
                  
                    int[] line = allReaded.Split(' ')
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => int.Parse(x)).ToArray(); //transformation string to the int[]

                    if (line.Length == 0) //if file don't correct
                    {
                        MessageBox.Show("Встречена пустая строка. Загрузка не удалась");
                        return;
                    }

                    int[] oneTaskOnDevices = new int[line.Length - 2]; //array with line about 1 task
                    Array.Copy(line, oneTaskOnDevices, line.Length - 2); //copy necessary information

                    tasksOnDevices.Add(new List<int>(oneTaskOnDevices)); // add line about 1 task to the massive
                    sizeDataInTasks.Add(line[line.Length - 2]); //add size of data 1 task
                    deadlinesOfTasks.Add(line[line.Length - 1]); 
                }
                


                fileWithData.Close();
            }


        }

        public void btnStartCalculating(object sender, RoutedEventArgs e)
        {
            if(tasksOnDevices.Count == 0)
            {
                MessageBox.Show("Данные для расчетов не были загружены");
                return;
            }

            //id of the best device
            int bestDevice;
            //<id_zaprosa, id_device>
            Dictionary<int, int> fastestDevices = new Dictionary<int, int>();
            //время, когда каждый прибор освободится
            int[] timeOfDeviceRelease = new int[tasksOnDevices[0].Count];
            //временный массив из которого будут исключаться задания
            var tempTasksOnDevices = new List<List<int>>(tasksOnDevices);
            //самое быстровыполняемое задание
            int fastestTask;
            //время выполнения самого быстровыполняемого задания
            int timeOfFastestTask;
            //счётчик для подсчета номеров запросов на приборах
            int[] numbersTasksOnDevices = Array.ConvertAll(new int[tasksOnDevices[0].Count], x => x + 1);

            //while (tempTasksOnDevices.Count !=0)
            while (sheduleOfProcessing.Count != tasksOnDevices.Count)
            {
                fastestDevices.Clear();
                //для каждого задания находится прибор, который быстрее всего их отработает
                //for each task find device that can do it task the fastest
                foreach (var task in tempTasksOnDevices)
                {
                    //if task exists in shedule
                    if (sheduleOfProcessing.ContainsKey(tempTasksOnDevices.IndexOf(task))) continue;
                    //id of the best device to this task
                    bestDevice = 0;
                    //у одного задания происходит пересмотр всех приборов и выбор самого эффективного для выполнения
                    //look at all devices, calculate time of ending of the real device and search the fastest device
                    for (var device = 0; device < tasksOnDevices[0].Count; device++)
                    {
                        if (timeOfDeviceRelease[device] + task[device] < timeOfDeviceRelease[bestDevice] + task[bestDevice])
                        {
                            bestDevice = device;
                        }
                    }

                    //add to the massive: id_task => bestDevice
                    fastestDevices.Add(tasksOnDevices.IndexOf(task), bestDevice);
                }

                fastestTask = 0;
                timeOfFastestTask = int.MaxValue;
                foreach (var task in fastestDevices)
                {
                   if(timeOfDeviceRelease[task.Value] + tasksOnDevices[task.Key][task.Value] <timeOfFastestTask)
                   {
                        fastestTask = task.Key;
                        timeOfFastestTask = timeOfDeviceRelease[fastestDevices[fastestTask]] + tasksOnDevices[task.Key][task.Value];
                    }
                }
                //в расписание записывается прибор и время завершения по ключу - номер запроса
                sheduleOfProcessing.Add(
                    fastestTask, 
                    new int[] 
                    { 
                        fastestDevices[fastestTask], 
                        numbersTasksOnDevices[fastestDevices[fastestTask]]++,
                        timeOfFastestTask - tasksOnDevices[fastestTask][fastestDevices[fastestTask]],
                        timeOfFastestTask 
                    });
                timeOfDeviceRelease[fastestDevices[fastestTask]] += timeOfFastestTask;
               
            }

           
            //создание расписания загрузки в хранилища

            //1. найти все запросы, которые первые на приборах 
            //2. добавить их в расписание на различные устройства первыми соотвественно
            //(иначе ограничение мат модели выполняться не будет)
            //3. расставить все запросы по эвристическому правилу, которые вмещаются
            //4. освобождать память в тот момент времени, когда запрос выполнится
            //5. шагать по расписанию, искать первый освободившийся запрос и уменьшать занятость хранилища 
            //6. после очищения запроса пытаться загрузить данные для нового запроса
            //7. повторять данное пока данные для всех запросов не будет размещены

            //расставляем запросы, первые в обработке
            foreach(var task in sheduleOfProcessing)
            {
                //если запрос является первым, то его данные грузятся в хранилище с номером прибора ( для упрощения кода)
                if(task.Value[1] == 1)
                {
                    sheduleOfLoading.Add(
                        task.Key, 
                        new int[] 
                        {
                            task.Value[0],
                            1,
                            0
                        });
                }
            }
        }

        public void btnOpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            settings settingsWindow = new settings();
            settingsWindow.Owner = this;
            settingsWindow.Show();
        }

        public void btnCloseProgram(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
