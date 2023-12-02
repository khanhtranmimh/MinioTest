using System.Collections.Generic;

namespace MinioTest
{
    public interface Subject
    {
        /// <summary>
        /// Both of these methods take an Observer as argument, that is
        /// the Observer to be registered or removed
        /// </summary>
        /// <param name="o"></param>
        public void registerObserver(Observer o);

        /// <summary>
        /// remove Observer
        /// </summary>
        /// <param name="o"></param>
        public void removeObserver(Observer o);


        /// <summary>
        /// This method is called to notify all observers
        /// when Subject's state has changed
        /// </summary>
        /// <param name="o"></param>
        public void notifyObservers();
    }

    /// <summary>
    /// The Observer interface is implemented by all observers, so they all have 
    /// implement the update() method.
    /// Here we're following Mary and Sue's lead 
    /// and passing the measurements to the Observers
    /// </summary>
    public interface Observer
    {
        /// <summary>
        /// these are the state values the Observers get from
        /// the Subject when a weather measurement changes
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="humididty"></param>
        /// <param name="pressure"></param>
        public void update(float temp, float humididty, float pressure);
    }

    /// <summary>
    /// The DisplayElement interface just includes one method display(),
    /// that we will call when the display element needs to be displayed
    /// </summary>
    public interface DisplayElement
    {
        public void display();
    }

    /// <summary>
    /// WeatherData now implements the Subject interface
    /// </summary>
    public class WeatherData : Subject
    {
        private List<Observer> observers;
        private float temperature;
        private float humidity;
        private float pressure;

        public WeatherData()
        {
            // we've added an List to hold the Observers
            // and we create it in the constructor
            observers = new List<Observer>();
        }

        /// <summary>
        /// Here's the fun part
        /// this is where we tell all the observers about the state.
        /// Because they are all Observers, we know they all implement update()
        /// so we know how to notify them
        /// </summary>
        /// <param name="o"></param>
        public void notifyObservers()
        {
            foreach (var ob in observers)
            {
                ob.update(temperature, humidity, pressure);
            }
        }

        /// <summary>
        /// When an observer registers, we just all it to end of the list
        /// </summary>
        /// <param name="o"></param>
        public void registerObserver(Observer o)
        {
            observers.Add(o);
        }

        /// <summary>
        /// Likewise, when an observer wants to unregister, we just take it off the list
        /// </summary>
        /// <param name="o"></param>
        public void removeObserver(Observer o)
        {
            observers.Remove(o);
        }

        /// <summary>
        /// we notify the Observers when we get updated measurements from the Weather Station
        /// </summary>
        public void measurementsChanged()
        {
            notifyObservers();
        }

        /// <summary>
        /// Okey, while we wanted to ship a nice little weather station with each book
        /// The publisher wouldn't go for it
        /// so, rather than reading actual weather data offa device,
        /// we're going to use this method to test our display elements.
        /// Or, for fun, you could write code to grab measurements off the web
        /// </summary>
        /// <param name="temperature"></param>
        /// <param name="humidity"></param>
        /// <param name="pressure"></param>
        public void setMeasurements(float temperature, float humidity, float pressure)
        {
            this.temperature = temperature;
            this.humidity = humidity;
            this.pressure = pressure;
            measurementsChanged();
        }

        // other weatherData methods here
    }

    public class ObserverPattern
    {
    }
}
