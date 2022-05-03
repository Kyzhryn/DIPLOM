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


        public MainWindow()
        {
            InitializeComponent();
            tasksOnDevices = new List<List<int>>();
            sizeDataInTasks = new List<int>();
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
                    //MessageBox.Show(allReaded);
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


        public void btnOpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            settings settingsWindow = new settings();
            settingsWindow.Show();
        }


    }
}
