using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//Interface is the addPersonTime(int id, int time) and returnPeople will return a list of 
// PersonTime objects - which associate tracking ID's with an inner list of timestamps




namespace ExpandableClass
{
    public class PersonTime
    {
        List<TimeSpan> timestamps = new List<TimeSpan>();
        public ulong TrackingID {get; set;}

        public PersonTime(ulong ID, TimeSpan time)
        {
            TrackingID = ID;
            timestamps.Add(time); 
        }

        public void addTime(TimeSpan time)
        {
            timestamps.Add(time); 
        }

    }

    public class ExpandablePersonTimes
    {
        List<PersonTime> people = new List<PersonTime>();

        public void addPersonTime(ulong trackingID, TimeSpan time)
        {
            PersonTime newPerson = new PersonTime(trackingID, time);

            if (people.BinarySearch(newPerson) < 0) //not in list, add the new person
            {
                people.Add(newPerson);
            }
            else  //otherwise, found, add the timestamp
            {
                people[people.BinarySearch(newPerson)].addTime(time); 
            }

        }

        List<PersonTime> returnPeople() //accesses a list of people w/ timestamps
        {
            return people; 
        }

    }



}
