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
            string allSettings = count.Text + " " + size.Text;
            int countStorage;
            int sizeStorage;

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

            //set values to the global space
            App.Current.Properties["countStorage"] = countStorage;
            App.Current.Properties["sizeStorage"] = sizeStorage;

            //write in file
            StreamWriter writerSettings = new StreamWriter("settings.txt");
            writerSettings.WriteLine(allSettings);
            writerSettings.Close();
            //and close window
            this.Close();

        }

    }
}
