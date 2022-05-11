using System.IO;
using System.Windows;

namespace DIPLOM
{
    /// <summary>
    /// Логика взаимодействия для settings.xaml
    /// </summary>
    public partial class settings : Window
    {
        public settings()
        {
            InitializeComponent();
        }

        public void btnSaveSettings(object sender, RoutedEventArgs e)
        {
            string allSettings = count.Text + " " + size.Text + " " + speed.Text;
            int countStorage;
            int sizeStorage;
            int speedLoad;

            if (!int.TryParse(count.Text, out countStorage))
            {
                MessageBox.Show("Вы ввели не число в поле количество");
                return;
            }
                
            if (!int.TryParse(size.Text, out sizeStorage))
            {
                MessageBox.Show("Вы ввели не число в поле размера хранилища");
                return;
            }

            if((!int.TryParse(speed.Text,out speedLoad)))
            {
                MessageBox.Show("Вы ввели не число в поле скорость загрузки");
            }

            //set values to the global space
            App.Current.Properties["countStorage"] = countStorage;
            App.Current.Properties["sizeStorage"] =  sizeStorage;
            App.Current.Properties["speed"]       =  speedLoad;
            //write in file
            StreamWriter writerSettings = new StreamWriter("settings.txt");
            writerSettings.WriteLine(allSettings);
            writerSettings.Close();
            //and close window
            this.Close();

        }

    }
}
