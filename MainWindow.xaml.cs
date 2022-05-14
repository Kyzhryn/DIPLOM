using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;


namespace DIPLOM
{

    public static class DictionaryExtensions
    {
        public static TKey MaxIndex<TKey>(this IDictionary<TKey, int[]> dictionary)
        {
            return dictionary.FirstOrDefault(x => x.Value == dictionary.Values.Max()).Key;
        }
    }

    public partial class MainWindow : Window
    {

        /// <summary>
        /// size of data in i request
        /// </summary>
        private List<int> _sizeDataInTasks;

        /// <summary>
        /// deadlines of each 
        /// </summary>
        private List<int> _deadlinesOfTasks;

        /// <summary>
        /// time of i request processing on the j device
        /// </summary>
        private List<List<int>> _tasksOnDevices;

        /// <summary>
        /// расписание загрузки на приборы: ключ - номер запроса,
        ///в массиве
        ///[
        ///номер прибора,
        ///порядковый номер на приборе,
        ///время начала обработки,
        ///время окончания обработки
        /// </summary>
        public Dictionary<int, int[]> _sheduleOfProcessing;

        /// <summary>
        /// расписание загрузки данных для запросов в хранилища
        /// ключ - номер запроса, в масссиве
        ///[
        ///номер хранилища,
        ///номер в загрузке на данном хранилище,
        ///момент начала загрузки
        ///]
        /// </summary>
        public Dictionary<int, int[]> _sheduleOfLoading;

        /// <summary>
        /// настройки по умолчанию для хранилищ
        /// </summary>
        public static string _defaultSettings = "5 500";

        /// <summary>
        /// имя файла для хранения настроек
        /// </summary>
        public static string _fileNameWithSettings = "settings.txt";
        public MainWindow()
        {
            int[] massiveWithSettings;
            string readedSettings = "";
            //check exists file
            if (!File.Exists(_fileNameWithSettings)) //if not exist
            {
                //write and create file
                StreamWriter writerSettings = new StreamWriter(_fileNameWithSettings);
                writerSettings.WriteLine(_defaultSettings);
                writerSettings.Close();

                //split defaultSettings string and get countStorage and sizeStorage
                massiveWithSettings = _defaultSettings
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
                StreamReader fileReadSettings = new StreamReader(_fileNameWithSettings);
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
            App.Current.Properties["sizeStorage"]  = massiveWithSettings[1];
            App.Current.Properties["speed"]        = massiveWithSettings[2];

            _tasksOnDevices       = new List<List<int>>();
            _sizeDataInTasks      = new List<int>();
            _sheduleOfProcessing  = new Dictionary<int, int[]>();
            _sheduleOfLoading     = new Dictionary<int, int[]>();
            _deadlinesOfTasks     = new List<int>();
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

                    _tasksOnDevices.Add(new List<int>(oneTaskOnDevices)); // add line about 1 task to the massive
                    _sizeDataInTasks.Add(line[line.Length - 2]); //add size of data 1 task
                    _deadlinesOfTasks.Add(line[line.Length - 1]); 
                }
                


                fileWithData.Close();
            }


        }

        public void btnStartCalculating(object sender, RoutedEventArgs e)
        {
            if(_tasksOnDevices.Count == 0)
            {
                MessageBox.Show("Данные для расчетов не были загружены");
                return;
            }
            
            // id of the best device
            int bestDevice;
            //<id_zaprosa, id_device>
            Dictionary<int, int> fastestDevices = new Dictionary<int, int>();
            //время, когда каждый прибор освободится
            int[] timeOfDeviceRelease = new int[_tasksOnDevices[0].Count];
            //самое быстровыполняемое задание
            int fastestTask;
            //время выполнения самого быстровыполняемого задания
            int timeOfFastestTask;
            //счётчик для подсчета номеров запросов на приборах
            int[] numbersTasksOnDevices = Array.ConvertAll(new int[_tasksOnDevices[0].Count], x => x + 1);

            while (_sheduleOfProcessing.Count != _tasksOnDevices.Count)
            {
                fastestDevices.Clear();
                //для каждого задания находится прибор, который быстрее всего их отработает
                //for each task find device that can do it task the fastest
                foreach (var task in _tasksOnDevices)
                {
                    //if task exists in shedule
                    if (_sheduleOfProcessing.ContainsKey(_tasksOnDevices.IndexOf(task))) continue;
                    //id of the best device to this task
                    bestDevice = 0;
                    //у одного задания происходит пересмотр всех приборов и выбор самого эффективного для выполнения
                    //look at all devices, calculate time of ending of the real device and search the fastest device
                    for (var device = 0; device < _tasksOnDevices[0].Count; device++)
                    {
                        if (timeOfDeviceRelease[device] + task[device] < timeOfDeviceRelease[bestDevice] + task[bestDevice])
                        {
                            bestDevice = device;
                        }
                    }

                    //add to the massive: id_task => bestDevice
                    fastestDevices.Add(_tasksOnDevices.IndexOf(task), bestDevice);
                }

                fastestTask = 0;
                timeOfFastestTask = int.MaxValue;
                foreach (var task in fastestDevices)
                {
                   if(timeOfDeviceRelease[task.Value] + _tasksOnDevices[task.Key][task.Value] <timeOfFastestTask)
                   {
                        fastestTask = task.Key;
                        timeOfFastestTask = timeOfDeviceRelease[fastestDevices[fastestTask]] + _tasksOnDevices[task.Key][task.Value];
                   }
                }
                //в расписание записывается прибор и время завершения по ключу - номер запроса
                _sheduleOfProcessing.Add(
                    fastestTask, 
                    new int[] 
                    { 
                        fastestDevices[fastestTask], 
                        numbersTasksOnDevices[fastestDevices[fastestTask]]++,
                        timeOfFastestTask - _tasksOnDevices[fastestTask][fastestDevices[fastestTask]],
                        timeOfFastestTask 
                    });
                timeOfDeviceRelease[fastestDevices[fastestTask]] += timeOfFastestTask;
               
            }




            //создание расписания загрузки в хранилища

            int countStorages = (int)App.Current.Properties["countStorage"];
            //словарь для подсчёта заполнения хранилищ
            Dictionary<int, int[]> containersToData = new Dictionary<int, int[]>();
            //заполняем его записями: номер хранилища => объём занятых данных, время окончания загрузки, кол-во элементов на загрузку
            //вначале все хранилища пусты
            for(var i =0; i<countStorages; i++ )
            {
                containersToData.Add(i,new int[] { 0, 0, 0 });
            }
            // изменить тип containersToData на словарь, заполнить его от 0 до (int)App.Current.Properties["countStorage"]] - 1 нулями 
            // пример перебора отсортированного
            // foreach (var pair in dict.OrderBy(pair => pair.Value))
            // {
            //   Console.WriteLine("{0} - {1}", pair.Key, pair.Value);
            // }
            

            int speed              = (int)App.Current.Properties["speed"];
            int sizeStorage        = (int)App.Current.Properties["sizeStorage"];
            //1. найти все запросы, которые первые на приборах 
            //2. добавить их в расписание на различные устройства первыми соотвественно
            //(иначе ограничение мат модели выполняться не будет)
            //3. расставить все запросы по эвристическому правилу, которые вмещаются
            //4. освобождать память в тот момент времени, когда запрос выполнится
            //5. шагать по расписанию, искать первый освободившийся запрос и уменьшать занятость хранилища 
            //6. после очищения запроса пытаться загрузить данные для нового запроса
            //7. повторять данное пока данные для всех запросов не будет размещены

            //расставляем запросы, первые в обработке
            #region

            foreach (var task in _sheduleOfProcessing)
            {
                //если запрос является первым, то его данные грузятся в хранилище с номером прибора ( для упрощения кода)
                if(task.Value[1] == 1)
                {
                    //для тех запросов, которые обрабатываются первыми на устройствах
                    //данные грузятся на различные хранилища первыми
                    _sheduleOfLoading.Add(
                        task.Key, 
                        new int[] 
                        {
                            task.Value[0],                    //номер хранилища
                            1,                                //порядковый номер загрузки в хранилище
                            0,                                //время начала загрузки
                            _sizeDataInTasks[task.Key] * speed //время окончания загрузки
                        });

                    //заполняем контейнеры данными
                    containersToData[task.Value[0]][0] += _sizeDataInTasks[task.Key];

                    //записываем время окончания загрузки в данное хранилище
                    containersToData[task.Value[0]][1] += _sizeDataInTasks[task.Key] * speed;

                    //увеличиваем кол-во загружаемых запросов в хранилище
                    containersToData[task.Value[0]][2]++;
                }
            }
            #endregion

            //если расставлены данные для всех запросов, то выйти
            #region
            if (_sheduleOfLoading.Count == _sheduleOfProcessing.Count)
            {
                MessageBox.Show("Обработка окончена");
                return;
            }
            #endregion

            //флаг для определения нашелся ли запрос,
            //данные которого сейчас помещаются в какое-либо хранилище
            // var taskFinding = false;

            //айди наименее объёмного подходящего задания
            //подходящий - время начала обработки больше, чем время начала загрузки
            var taskWithMinData = 0;

            //объём наименьшего запроса
            var sizeOfTaskWithMindata = int.MaxValue;

            //айди наиболее заполненного хранилища
            var leastEmptyStorage = 0;

            //заполнение хранилищ данными для запросов доверху
            #region
            while (true) //цикл будет работать
            {
                
                taskWithMinData = 0;

                //проверка на расстановку ресурсов для всех запросов
                #region 
                //данные для всех запросов поставлены в расписание 
                if (_sheduleOfLoading.Count == _sheduleOfProcessing.Count)
                {
                    MessageBox.Show("Обработка окончена");
                    return;
                }
                #endregion


                //определение наиболее заполненного хранилища
                #region
                leastEmptyStorage = 0;
                var max = 0;
                foreach(var storage in containersToData)
                {
                    if (storage.Value[0] > max)
                    {
                        max = storage.Value[0];
                        leastEmptyStorage = storage.Key;
                    }
                }
                sizeOfTaskWithMindata = int.MaxValue;
                #endregion

                //обход всех запросов и поиск наименьшего подходящего
                #region
                for (var data = 0; data < _sizeDataInTasks.Count; data++ )
                {
                    //если данные запроса уже размещены в расписании, его нужно пропустить
                    if(_sheduleOfLoading.ContainsKey(data))
                    {
                        continue;
                    }

                    //если запрос требует ресурсов меньше,
                    //чем текущий наименьший, то теперь он наименьший
                    //так же время обработки должно быть больше времени начала загрузки
                    if(_sizeDataInTasks[data] < sizeOfTaskWithMindata 
                        && 
                        _sheduleOfProcessing[data][2] > containersToData[leastEmptyStorage][1])
                    {
                        taskWithMinData = data;
                        sizeOfTaskWithMindata = _sizeDataInTasks[data];
                    }
                }
                #endregion

                //если размер наименьшего запроса слишком велик, чтобы уместиться в хранилище
                #region
                if (sizeOfTaskWithMindata > sizeStorage - containersToData[leastEmptyStorage][0])
                {
                    //обойти расписание обработки,
                    //найти запрос,который закончит обработку раньше всех,
                    //удалить объём его данных из его хранилища,
                    //пометить каким-то образом запрос как освобожденный 
                    //сделать continue
                }
                #endregion


                //добавить в расписание запись о размещении запроса taskWithMinData в хранилище leastEmptyStorage
                #region
                _sheduleOfLoading.Add(
                        taskWithMinData,
                        new int[]
                        {
                           leastEmptyStorage,                            //номер хранилища
                           containersToData[leastEmptyStorage][2],     //порядковый номер загрузки в хранилище
                           containersToData[leastEmptyStorage][1],       //время начала загрузки
                           containersToData[leastEmptyStorage][1] + _sizeDataInTasks[taskWithMinData] * speed   //время окончания загрузки
                        });
                #endregion

                //обновить данные о хранилище
                #region

                //заполняем контейнеры данными
                containersToData[leastEmptyStorage][0] += _sizeDataInTasks[taskWithMinData];

                //записываем время окончания загрузки в данное хранилище
                containersToData[leastEmptyStorage][1] += _sizeDataInTasks[taskWithMinData] * speed;

                //увеличиваем кол-во загружаемых запросов в хранилище
                containersToData[leastEmptyStorage][2]++;
                #endregion
            }
            #endregion
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
