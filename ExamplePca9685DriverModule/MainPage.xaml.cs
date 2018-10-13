using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExamplePca9685DriverModule
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Pca9685 _pca9685 = new Pca9685(20);

        private DispatcherTimer _timer = new DispatcherTimer();

        public MainPage() => this.InitializeComponent();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                await this.Run();
            });
        }

        private async Task Run()
        {
            await this._pca9685.Start();
            this.TextBlockInformations.Text = "Driver module initialized";
            this.TextBlockServerControlState.Text = "wait...";

            while (true)
            {
                for (int i = 7; i < 8; i++)
                {
                    Debug.WriteLine("Output: " + i.ToString());
                    this.TextBlockServerControlState.Text = $"Output: {i}";

                    for (int high = 3640; high < 3940; high++)
                    {
                        this._pca9685.SetPwm((byte)i, high, 0);
                        await Task.Delay(20);
                    }

                    // other way
                    for (int low = 150; low < 400; low++)
                    {
                        this._pca9685.SetPwm((byte)i, 0, low);
                        await Task.Delay(20);
                    }
                }
            }
        }
    }
}
