using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace symulowanie_wyżarzania_harmonogram
{
    public class SA_SchedulingProblem 
    {
        public List<int> currentOrder = new List<int>();    //inicjalizacja zmiennych, ktore beda wykorzystywane w trakcie pracy algorytmu
        private List<int> nextOrder = new List<int>();
        private Random random = new Random();
        public Queue<double> Items = new Queue<double>();
        public double bestScheme = 0;

      
        private void LoadItems()   //inicjalizacja zadan z pliku
        {
            string[] items = File.ReadAllLines("../../../zadania.txt");
            for (int i = 0; i < items.Length; i++)
            {
                Items.Enqueue(double.Parse(items[i]));
                currentOrder.Add(i+1);
            }
        }

        private double CountWeight(List<int> order)   //liczymy wydajnosc schematu wedlug przekazanej kolejki
        {
            double weight1 = 0, weight2 = 0;
            double maxweight = 0;
            ConcurrentQueue<double> localItems = new ConcurrentQueue<double>();
            double[] tmpItems = new double[Items.Count];
            Items.CopyTo(tmpItems, 0);
            foreach (var tmp in tmpItems)
                localItems.Enqueue(tmp);                    //dodajemy nasze zadania do kolejki przetwarzania

            Parallel.Invoke(                                //2 watka - 2 pracownika, ewentulanie można na poziomie kodu dodać ich więcej
            () => {weight1 = HelpMethod();},
            () => {weight2 = HelpMethod();}
            );

            double HelpMethod()                             //metoda do zdejmowania zadan z kolejki przetwarzania
            {
                double weight = 0;
                for (int i = 0; i < order.Count; i++)
                {
                    if (localItems.Count > 0)
                    {
                        double result;
                        localItems.TryDequeue(out result);
                        if (result != 0)
                            weight += result;               //liczymy wage przetwarzonych zadan dla kazdego pracownika
                    }
                }
                return weight;
            }

            return maxweight = weight1 > weight2 ? weight1 : weight2;
        }

        private List<int> Swap(List<int> order)             //zmeniamy losowo 2 pozycji zadan w kolejce 
        {
            List<int> newOrder = new List<int>();

            for (int i = 0; i < order.Count; i++)
                newOrder.Add(order[i]);

            int RandomItem1 = random.Next(0, newOrder.Count);  //losujemy 2 pozycji zadan
            int RandomItem2 = random.Next(0, newOrder.Count);

            int tmp = newOrder[RandomItem1];
            newOrder[RandomItem1] = newOrder[RandomItem2];      //zamieniamy miejscamu te 2 pozycji
            newOrder[RandomItem2] = tmp;

            return newOrder;
        }

        public void Anneal()    //wyzarzanie 
        {
            double temperature = 10000.0;   //podajemu temperature
            double deltaWeight = 0;
            double cooling = 0.9999;       //podajemy wspolczynnik ochlodzenia 
            double eps = 0.00001;           //nasz epsilon, czyli wartosc koncowa dla naszej temperatury, robimy ochlodzienia, az poki nie dojdziemy do niej

            LoadItems();    //inicjalizujemy zadania

            double weightTMP = CountWeight(currentOrder);   //obliczamy wydajnosc pierwszy raz

            while (temperature > eps)   //petla wyzarzania
            {
                nextOrder = Swap(currentOrder);   //robimy zamiane 2 losowych zadan

                deltaWeight = CountWeight(nextOrder) - weightTMP;    //liczymy wydajnosc schematu po tej zamianie
                
                if ((deltaWeight < 0) || (weightTMP > 0 && Math.Exp(-deltaWeight / temperature) > random.NextDouble())) //jesli po zmianie kolejnosci tych zadan schemat harmonogramu jest lepszy, to zastapiamy nim stary
                {
                    for (int i = 0; i < nextOrder.Count; i++)
                        currentOrder[i] = nextOrder[i];         //jesli powyzszy warunek jest spelniony, to na obecna chwile mamy lepszy schemat, wiec zapisujemy jego kolejnosc

                    weightTMP = deltaWeight + weightTMP;        //oraz zapisujemy jego wydajnosc
                }
              
                temperature *= cooling; //zmniejszamy temperature
            }

            bestScheme = weightTMP;     //w koncu, majac najlepszy schemat, przypisujemy jego do zmiennej publicznej
        }

        public override string ToString()
        {
            string result = "";
            short n;

            for (int i = 0; i < currentOrder.Count; i++)       //ladnie formatujemy wynik koncowy
            {
                if (i % 2 == 0) n = 1;
                else n = 2;
                result += String.Format("Worker {0}: {1}\n", n, currentOrder[i]);
            }

            string totalItems = "Total Items to do: " + currentOrder.Count.ToString();  //ilosc zadan
            string defaultTime = "Default time (one worker): " + Items.Sum();            //wydajnosc w przypadku jednego pracownika
            string shortestScheme = "Shortest Scheme: \n" + result;                      //najlepszy schemat, ktory jest wynikiem naszego wyzarzania
            string bestTime = "The best time is (two workers): " + bestScheme.ToString(); //wydajnosc tego schematu

            string outputString = string.Format("{0}{1}{2}{3}{4}{5}{6}", totalItems, Environment.NewLine, defaultTime, Environment.NewLine, shortestScheme, Environment.NewLine, bestTime);
            return outputString;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SA_SchedulingProblem sa_schedulingproblem = new SA_SchedulingProblem(); //inicjalizujemy klase
            sa_schedulingproblem.Anneal();  //wyzarzamy
            Console.WriteLine(sa_schedulingproblem.ToString());
            
        }
    }
}

/*
 Jak bylo opisano w poprzednim zadaniu (symulowanie wyzarzania dla problemu komiwojazera) algorytm ten jest dosc prosty do zrozumienia i implementacji.
 Ale z innej strony, placimy za to niedokladnoscia wyniku, ktora rosnie przy zwiekszaniu liczby zadan. Teorytycznie mozemy zwiekszyc "temperature", co bedzie powodowalo
 zwiekszenie ilosci cyklow petli wyzarzania, ale podobnie jak z wyzarzaniem metalow: z czasem, im mniejsza jest temperatura - tym mniej jest prawdobodobienstwo udanej zamiany atomow.
 Wiec, w moim zdaniu, zmiekszewnie ilosci cyklow na duza liczbe nie jest oplacalnym w stosunku do wydajnosci pracy algorytmu. Naprzyklad, w moim przypadku ja probowalem uzyc
 10000.0 oraz 10000000.0: w obu przypadkach mialem prawie takie same dokladnosci, ale w drugim - znacznie wolniej.
*/
