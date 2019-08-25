﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT323Assignment1
{
    public class Allocation
    {
        #region Properties
        public int ID { get; set; }
        public int[,] AllocationMatrix { get; set; }
        public Dictionary<int, double> processorTimes = new Dictionary<int, double>();
        public double AllocationTime;
        public double AllocationEnergy;


        private const string invalidIDError = "Invalid ID on Allocation with ID: ";
        private const string multipleAllocationError = "A task ID: {0} in allocation ID: {1} has been allocated to {2} processors instead of 1";
        private const string noAllocationError = "Task ID: {0} in allocation ID: {1} is not allocated to any processor";
        private const string exceedMaxRuntimeError = "Allocation ID: {0} runtime is {1:0.00} seconds which is greater than the max runtime of {2} seconds";


        public bool isValid = true;
        #endregion

        #region Constructors
        public Allocation(int id, int[,] matrix)
        {
            ID = id;
            AllocationMatrix = matrix;
        }

        public Allocation(int id, List<string> matrix)
        {
            ID = id;

            int rows = matrix.Count;
            int columns = matrix[1].Replace(",", "").Length;
            AllocationMatrix = new int[rows,columns];

            for (int i = 0 ; i < rows; i++)
            {
                for(int j = 0; j < columns; j++)
                {
                    string[] substrings = matrix[i].Split(',');
                    int input = ToInt32(substrings[j]);
                    if (input == -1)
                    {
                       //error
                    }
                    else
                    {
                        AllocationMatrix[i,j] = input;
                    }
                   
                }
            }
        }
        #endregion

        #region Methods
        public static int ToInt32(string input)
        {
            if (Int32.TryParse(input, out int anInt))
            {
                return anInt;
            }
            else
            {
                return -1;
            }

        }

        public override string ToString()
        {
            string str = "ID: " + ID + "\n";

            for (int i = 0; i < AllocationMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AllocationMatrix.GetLength(1); j++)
                {
                    str = str + AllocationMatrix[i,j] + ",";
                }

                //Remove last , 
                str = str.Substring(0, str.Length - 1);
                str = str + "\n";
            }

            //Remove last \n 
            str = str.Substring(0, str.Length - 1);

            return str;
        }

        public string MatrixToString()
        {
            string str = "";

            for (int i = 0; i < AllocationMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < AllocationMatrix.GetLength(1); j++)
                {
                    str += AllocationMatrix[i, j] + ",";
                }

                //Remove last , 
                str = str.Substring(0, str.Length - 1);
                str += "\n";
            }
            str += "\n";

            return str;
        }

        public bool ValidateAllocation(out List<string> errors)
        {
            errors = new List<string>();

            //Check ID is valid
            if(ID < 0)
            {
                errors.Add(invalidIDError + ID);
                isValid = false;
            }

            int rows = AllocationMatrix.GetLength(0);
            int columns = AllocationMatrix.GetLength(1);

            for(int tasks = 0; tasks < columns; tasks++)
            {
                int columnSum = 0;
                for(int processors = 0; processors < rows; processors++)
                {
                    //Add each column to determine if there is more or less than one 1 
                    columnSum += AllocationMatrix[processors, tasks];
                    string log = string.Format("Task: {0}   Pro: {1}  Sum: {2}", tasks, processors, columnSum);
                }

                
                if(columnSum != 1)
                {
                    //errors.Add(invalidAllocationError + ToString());
                    isValid = false;
                    if (columnSum == 0) errors.Add(string.Format(noAllocationError, tasks + 1, ID));
                    if (columnSum > 1) errors.Add(string.Format(multipleAllocationError, tasks + 1, ID, columnSum));
                    
                }
                
            }

            return isValid;
        }

        public double CalculateTime(Configuration aconfiguration, out List<string> errors)
        {
            errors = new List<string>();
            double time = 0;
            int[,] runtimeMatrix = AllocationMatrix.Clone() as int[,];
            int rows = runtimeMatrix.GetLength(0);
            int columns = runtimeMatrix.GetLength(1);

            //add runtimes to allocation matrix
            if (aconfiguration.TaskRuntimes != null)
            {
                foreach (KeyValuePair<int, int> task in aconfiguration.TaskRuntimes)
                {
                    for (int processors = 0; processors < rows; processors++)
                    {
                        if (runtimeMatrix[processors, (task.Key - 1)] == 1)
                        {
                            runtimeMatrix[processors, task.Key - 1] = task.Value;
                        }
                    }
                }

                //Calculate time for each processor
                for (int processors = 0; processors < rows; processors++)
                {
                    int rowSum = 0;
                    for (int tasks = 0; tasks < columns; tasks++)
                    {
                        rowSum += runtimeMatrix[processors, tasks];
                    }
                    double processorTime = rowSum * (aconfiguration.RuntimeReferenceFrequency / aconfiguration.ProcessorFrequencies[processors + 1]);
                    processorTimes.Add(processors + 1, processorTime);
                    if (processorTime > time) time = processorTime;
                }
            }

            if (time > aconfiguration.ProgramMaxDuration) errors.Add(string.Format(exceedMaxRuntimeError, ID, time, aconfiguration.ProgramMaxDuration));

            AllocationTime = time;
            return time;
        }

        public double CalculateEnergy(Configuration aConfiguration, out List<string> errors)
        {
            errors = new List<string>();
            double energy = 0;

            double c0 = aConfiguration.CoefficientValues[0];
            double c1 = aConfiguration.CoefficientValues[1];
            double c2 = aConfiguration.CoefficientValues[2];

            foreach(KeyValuePair<int, double> time in processorTimes)
            {
                
                double f = aConfiguration.ProcessorFrequencies[time.Key];
                energy += time.Value * (c2 * (f * f) + c1 * f + c0);
                
            }

            AllocationEnergy = energy;
            return energy;
        }
        #endregion
    }
}
