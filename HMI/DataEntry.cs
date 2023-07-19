namespace Prova
{
    public class DataEntry
    {
        private bool status;
        private readonly double unixtimestamp;
        private Status1 status1;

        public DataEntry(double unixtimestamp, bool status, Status1 status1)
        {
            this.status = status;
            this.unixtimestamp = unixtimestamp;
            this.status1 = status1;
        }

        public bool get_status() { return this.status; }
        public double get_unixtimestamp() { return this.unixtimestamp; }
        public Status1 get_status1() { return this.status1; }
        public void set_status1(Status1 status1) { this.status1 = status1; }
        public void set_status(bool status) { this.status = status; }

    }
}