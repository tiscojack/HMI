using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HMI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LiveCharts.Configure(config =>
                config
                    // registers SkiaSharp as the library backend
                    // REQUIRED unless you build your own
                    .AddSkiaSharp()

                    // adds the default supported types
                    // OPTIONAL but highly recommend
                    .AddDefaultMappers()

                    // select a theme, default is Light
                    // OPTIONAL
                    //.AddDarkTheme()
                    .AddLightTheme()

                    // finally register your own mappers
                    // you can learn more about mappers at:
                    // https://lvcharts.com/docs/WPF/2.0.0-beta.800/Overview.Mappers
                    .HasMap<City>((city, point) =>
                    {
                        point.PrimaryValue = city.Population;
                        point.SecondaryValue = point.Index;
                    })
                // .HasMap<Foo>( .... ) 
                // .HasMap<Bar>( .... ) 
                );
        }

        public record City(string Name, double Population);
    }
}
