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

        private List<int> sizeDataInTasks; //size of data in i request
        private List<List<int>> tasksOnDevices;//time of i request processing on the j device
        public Dictionary<int, int[]> sheduleOfProcessing;

        public static string defaultSettings = "5 500";
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

            tasksOnDevices = new List<List<int>>();
            sizeDataInTasks = new List<int>();
            sheduleOfProcessing = new Dictionary<int, int[]>();

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
                while(fileWithData.EndOfStream != true)
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

                    int[] oneTaskOnDevices = new int[line.Length - 1]; //array with line about 1 task
                    Array.Copy(line, oneTaskOnDevices, line.Length - 1); //copy necessary information

                    tasksOnDevices.Add(new List<int>(oneTaskOnDevices)); // add line about 1 task to the massive
                    sizeDataInTasks.Add(line[line.Length - 1]); //add size of data 1 task
                }
                


                fileWithData.Close();
            }


        }


        public void btnStartCalculating(object sender, RoutedEventArgs e)
        {
            //int countStorage = (int)App.Current.Properties["countStorage"];

            if(tasksOnDevices.Count == 0)
            {
                MessageBox.Show("Данные для расчетов не были загружены");
                return;
            }

            //id of the best device
            int bestDevice;
            //time of the best device
            int minTime;

            //<id_zaprosa, id_device>
            Dictionary<int, int> fastestDevices = new Dictionary<int, int>();
             я забыл про директивный срок выполнения запроса, нужно добавить их в файл и добавить при считывании

            for (var task = 0; task < tasksOnDevices.Count; task++)
            {
               
                bestDevice = 0;
                
                minTime = tasksOnDevices[task][bestDevice];
                for(int device = 1; device <tasksOnDevices[task].Count; device++)
                {
                   if(tasksOnDevices[task][device] < tasksOnDevices[task][bestDevice]) bestDevice = device;
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
