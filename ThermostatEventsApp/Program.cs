using System.ComponentModel;

Console.WriteLine("Press any key to run the device... ");
Console.ReadKey();
IDevice device = new Device();

device.RunDevice();

Console.ReadKey();
public class Device:IDevice
{
    
    private const double WarningLevel = 27;
    private const double EmergencyLevel = 75;
    
    public double WarningTemperatureLevel => WarningLevel;
    public double EmergencyTemperatureLevel => EmergencyLevel;
    void IDevice.RunDevice()
    {
        Console.WriteLine("Device is running...");
        ICoolingMechanism coolingMechanism = new CoolingMechanism();
        IHeatSensor heatSensor = new HeatSensor(WarningLevel, EmergencyLevel);
        IThermostat thermostat = new Thermostat(this, coolingMechanism, heatSensor);
        thermostat.RunThermostat();
    }

    void IDevice.HandleEmergency()
    {
        Console.WriteLine();
        Console.WriteLine("Sending out notifications to emergency services personell");
        ShutDownDevice();
        Console.WriteLine();
    }

    private void ShutDownDevice()
    {
        Console.WriteLine("Shutting down device....");
    }
}

public class Thermostat : IThermostat
{
    private ICoolingMechanism _coolingMechanism = null;
    private IHeatSensor _heatSensor = null;
    private IDevice _device = null;

    private const double WarningLevel = 27;
    private const double EmergencyLevel = 75;


    public Thermostat(IDevice device, ICoolingMechanism coolingMechanism, IHeatSensor heatSensor)
    {
        _device = device;
        _coolingMechanism = coolingMechanism;
        _heatSensor = heatSensor;
    }

    private void WireUpEventsToEventHandlers()
    {
        _heatSensor.TemperatureReachesWarningLevelEventHandler += HeatSensorTemperatureReachesWarningLevelEventHandler;
        _heatSensor.TemperatureFallsBelowWarningLevelEventHandler +=
            HeatSensorTemperatureFallsBelowWarningLevelEventHandler;
        _heatSensor.TemperatureReachesEmergencyLevelEventHandler +=
            HeatSensorTemperatureReachesEmergencyLevelEventHandler;
    }

    private void HeatSensorTemperatureReachesWarningLevelEventHandler(object sender, TemperatureEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine();
        Console.WriteLine($"Warning Alert!! (Warning Level is between {_device.WarningTemperatureLevel} and {_device.EmergencyTemperatureLevel})");
        _coolingMechanism.On();
        Console.ResetColor();
    }

    private void HeatSensorTemperatureFallsBelowWarningLevelEventHandler(object sender, TemperatureEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine();
        Console.WriteLine($"Information Alert!! Temperature falls below warning level (Warning Level is between {_device.WarningTemperatureLevel} and {_device.EmergencyTemperatureLevel})");
        _coolingMechanism.Off();
        Console.ResetColor();
    }

    private void HeatSensorTemperatureReachesEmergencyLevelEventHandler(object sender, TemperatureEventArgs e) 
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine($"Emergency Alert!! (Emergency Level is {_device.EmergencyTemperatureLevel} and above.)");
        _device.HandleEmergency();
        Console.ResetColor();
    }


    public void RunThermostat()
    {
        Console.WriteLine("Thermostat is running...");
        WireUpEventsToEventHandlers();
        _heatSensor.RunHeatSensor();
    }
}

public interface IThermostat
{
    void RunThermostat();
}

public interface IDevice
{
    double WarningTemperatureLevel { get; }
    double EmergencyTemperatureLevel { get; }
    void RunDevice();
    void HandleEmergency();
}

public class CoolingMechanism : ICoolingMechanism
{
    public void On()
    {
        Console.WriteLine();
        Console.WriteLine("Switching cooling mechanism On...");
        Console.WriteLine();
    }

    public void Off()
    {
        Console.WriteLine();
        Console.WriteLine("Switching cooling mechanism Off...");
        Console.WriteLine();
    }
}

public interface ICoolingMechanism
{
    void On();
    void Off();
}

public class HeatSensor : IHeatSensor
{
    double _warningLevel = 0;
    double _emergencyLevel = 0;

    bool _hasReachedWarningTemperature = false;

    protected EventHandlerList _listEventDelegates = new EventHandlerList();

    static readonly object _temperatureReachesWarningLevelKey = new object();
    static readonly object _temperatureReachesEmergencyLevelKey = new object();
    static readonly object _temperatureFallsBelowWarningLevelKey = new object();

    private double[] _temperatureData = null;

    public HeatSensor(double warningLevel, double emergencyLevel)
    {
        _warningLevel = warningLevel;
        _emergencyLevel = emergencyLevel;
        SeedData();
    }

    public void MonitorTemperature()
    {
        foreach (var temperature in _temperatureData)
        {
            Console.ResetColor();
            Console.WriteLine($"DateTime: {DateTime.Now}, Temperature: {temperature}");

            if (temperature > _emergencyLevel)
            {
                TemperatureEventArgs e = new TemperatureEventArgs
                {
                    Temperature = temperature,
                    CurrentDateTime = DateTime.Now
                };
                OnTemperatureReachesEmergencyLevel(e);
            }
            else if (temperature >= _warningLevel)
            {
                _hasReachedWarningTemperature = true;
                TemperatureEventArgs e = new TemperatureEventArgs
                {
                    Temperature = temperature,
                    CurrentDateTime = DateTime.Now
                };
                OnTemperatureReachesWarningLevel(e);
            }
            else if (temperature < _warningLevel && _hasReachedWarningTemperature)
            {
                _hasReachedWarningTemperature = false;
                TemperatureEventArgs e = new TemperatureEventArgs
                {
                    Temperature = temperature,
                    CurrentDateTime = DateTime.Now
                };
                OnTemperatureFallsBelowWarningLevel(e);
            }

            System.Threading.Thread.Sleep(1000);
        }
    }

    private void SeedData()
    {
        _temperatureData = new double[] { 16, 17, 16.5, 18, 19, 22, 24, 26.75, 28.7, 27.6, 26, 24, 22, 45, 68, 86.45 };
    }

    protected void OnTemperatureReachesEmergencyLevel(TemperatureEventArgs e)
    {
        EventHandler<TemperatureEventArgs> handler =
            (EventHandler<TemperatureEventArgs>)_listEventDelegates[_temperatureReachesEmergencyLevelKey];
        handler?.Invoke(this, e);
    }

    protected void OnTemperatureReachesWarningLevel(TemperatureEventArgs e)
    {
        EventHandler<TemperatureEventArgs> handler =
            (EventHandler<TemperatureEventArgs>)_listEventDelegates[_temperatureReachesWarningLevelKey];
        handler?.Invoke(this, e);
    }

    protected void OnTemperatureFallsBelowWarningLevel(TemperatureEventArgs e)
    {
        EventHandler<TemperatureEventArgs> handler =
            (EventHandler<TemperatureEventArgs>)_listEventDelegates[_temperatureFallsBelowWarningLevelKey];
        handler?.Invoke(this, e);
    }

    event EventHandler<TemperatureEventArgs>? IHeatSensor.TemperatureReachesEmergencyLevelEventHandler
    {
        add { _listEventDelegates.AddHandler(_temperatureReachesEmergencyLevelKey, value); }
        remove { _listEventDelegates.RemoveHandler(_temperatureReachesEmergencyLevelKey, value); }
    }

    event EventHandler<TemperatureEventArgs>? IHeatSensor.TemperatureReachesWarningLevelEventHandler
    {
        add { _listEventDelegates.AddHandler(_temperatureReachesWarningLevelKey, value); }
        remove { _listEventDelegates.RemoveHandler(_temperatureReachesWarningLevelKey, value); }
    }

    event EventHandler<TemperatureEventArgs>? IHeatSensor.TemperatureFallsBelowWarningLevelEventHandler
    {
        add { _listEventDelegates.AddHandler(_temperatureFallsBelowWarningLevelKey, value); }
        remove { _listEventDelegates.RemoveHandler(_temperatureFallsBelowWarningLevelKey, value); }
    }

    public void RunHeatSensor()
    {
        Console.WriteLine("Heat sensor is running...");
        MonitorTemperature();
    }
}

public interface IHeatSensor
{
    event EventHandler<TemperatureEventArgs> TemperatureReachesEmergencyLevelEventHandler;
    event EventHandler<TemperatureEventArgs> TemperatureReachesWarningLevelEventHandler;

    event EventHandler<TemperatureEventArgs> TemperatureFallsBelowWarningLevelEventHandler;

    void RunHeatSensor();
}

public class TemperatureEventArgs : EventArgs
{
    public double Temperature { get; set; }
    public DateTime CurrentDateTime { get; set; }
}