﻿namespace DifferentialEquationSystem
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Expressions;
    using Expressions.Models;
    using System.Threading.Tasks;

    public partial class DifferentialEquationSystem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public DifferentialEquationSystem()
        { }

        /// <summary>
        /// Sets the initial parameters of the DifferentialEquationSystem class.
        /// </summary>
        /// <param name="expressions">List of expressions</param>
        /// <param name="leftVariables">List of left variables</param>
        /// <param name="constants">List of constants</param>
        /// <param name="timeVariable">Start time (presents in the expressions)</param>
        /// <param name="tEnd">End time</param>
        /// <param name="tau">Calculation step</param>
        public DifferentialEquationSystem(List<string> expressions, List<InitVariable> leftVariables,
            List<InitVariable> constants, InitVariable timeVariable, double tEnd, double tau)
        {
            // Setting up of variables and constants
            if (leftVariables != null)
            {
                this.LeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(leftVariables);
            }

            if (constants != null)
            {
                this.Constants = DifferentialEquationSystem.ConvertInitVariablesToVariables(constants);
            }

            if (timeVariable != null)
            {
                this.TimeVariable = timeVariable;
            }

            // Setting up of all variables
            this.AllVariables = new List<Variable>();
            if (this.LeftVariables != null)
            {
                this.AllVariables.AddRange(this.LeftVariables);
            }

            if (this.Constants != null && this.Constants.Count > 0)
            {
                this.AllVariables.AddRange(this.Constants);
            }

            if (this.TimeVariable != null)
            {
                this.AllVariables.Add(this.TimeVariable);
            }

            // Setting up of all expressions
            if (expressions == null || expressions.Count == 0)
            {
                throw new ArgumentException("Container 'expressions' of the constructor cannot be null or empty! Nothing in the differential equation system.");
            }
            else
            {
                List<Expression> expressionSystem = new List<Expression>();
                foreach (string expression in expressions)
                {
                    expressionSystem.Add(new Expression(expression, this.AllVariables));
                }

                this.ExpressionSystem = expressionSystem;
            }

            this.TEnd = tEnd;
            this.Tau = tau;

            DifferentialEquationSystem.CheckVariables(this.ExpressionSystem, this.LeftVariables, this.TimeVariable, this.Tau, this.TEnd);
        }

        /// <summary>
        /// Sets the initial parameters of the DifferentialEquationSystem class.
        /// </summary>
        /// <param name="expressionSystem">List of expressions</param>
        /// <param name="leftVariables">List of left variables</param>
        /// <param name="constants">List of constants</param>
        /// <param name="timeVariable">Start time (presents in the expressions)</param>
        /// <param name="tEnd">End time</param>
        /// <param name="tau">Calculation step</param>
        private DifferentialEquationSystem(List<Expression> expressionSystem, List<Variable> leftVariables,
            List<Variable> constants, Variable timeVariable, double tEnd, double tau)
        {
            this.ExpressionSystem = expressionSystem;
            this.LeftVariables = leftVariables;
            this.Constants = constants;
            this.TimeVariable = timeVariable;
            this.TEnd = tEnd;
            this.Tau = tau;
        }

        /// <summary>
        /// Gets or sets the list of the constant variables in the right part
        /// </summary>
        private List<Variable> Constants { get; set; }

        /// <summary>
        /// Gets or sets all variables considered in differential equation system
        /// </summary>
        private List<Variable> AllVariables { get; set; }

        /// <summary>
        /// Gets or sets the end time of the differental equation system calculation
        /// </summary>
        private double TEnd { get; set; }

        /// <summary>
        /// Gets or sets the calculation step
        /// </summary>
        private double Tau { get; set; }

        /// <summary>
        /// Gets or sets the list of Expressions
        /// </summary>
        private List<Expression> ExpressionSystem { get; set; }

        /// <summary>
        /// Gets or sets the list of left variables, presented in the differential equation system
        /// </summary>
        private List<Variable> LeftVariables { get; set; }        

        /// <summary>
        /// Gets or sets the time parameter if it exists in at least one differential equation
        /// </summary>
        public Variable TimeVariable { get; set; }

        /// <summary>
        /// Main method which performs a calculation
        /// </summary>
        /// <param name="calculationType">Name of the calculation method</param>
        /// <param name="results">Containier where result variables are supposed to be saved</param>
        /// <param name="variablesAtAllStep">Container of variables at each calculation step</param>
        /// <param name="async">Flag which specifies if it is calculated in parallel mode</param>
        /// <returns>Calculation time</returns>
        public double Calculate(CalculationTypeNames calculationType, out List<InitVariable> results, List<List<InitVariable>> variablesAtAllStep = null, bool async = false)
        {
            // Checking the correctness of input variables
            DifferentialEquationSystem.CheckVariables(this.ExpressionSystem, this.LeftVariables, this.TimeVariable, this.Tau, this.TEnd);

            Func<List<List<InitVariable>>, List<InitVariable>> F = this.DefineSuitableMethod(calculationType, async);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            results = F(variablesAtAllStep);
            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds / 1000.0;
        }

        public async Task<ConcurrentDictionary<CalculationTypeNames, double>> Calculate(List<CalculationTypeNames> calculationTypes, ConcurrentDictionary<CalculationTypeNames, 
            List<InitVariable>> results, ConcurrentDictionary<CalculationTypeNames, List<List<InitVariable>>> variablesAtAllSteps = null, bool async = false)
        {
            ConcurrentDictionary<CalculationTypeNames, double> timeResults = new ConcurrentDictionary<CalculationTypeNames, double>();

            // Checking the correctness of input variables
            DifferentialEquationSystem.CheckVariables(this.ExpressionSystem, this.LeftVariables, this.TimeVariable, this.Tau, this.TEnd);

            List<Task> calculationTasks = new List<Task>();

            foreach(CalculationTypeNames calculationType in calculationTypes)
            {
                Func<List<List<InitVariable>>, List<InitVariable>> F = this.DefineSuitableMethod(calculationType, async);
                
                Task calculationTask = new Task(() =>
                {                    
                    Stopwatch stopwatch = new Stopwatch();
                    List<InitVariable> localResult;
                    List<List<InitVariable>> variablesAtAllStepsForMethod = new List<List<InitVariable>>();

                    stopwatch.Start();
                    localResult = F(variablesAtAllStepsForMethod);
                    stopwatch.Stop();

                    results.TryAdd(calculationType, localResult);
                    timeResults.TryAdd(calculationType, stopwatch.ElapsedMilliseconds / 1000.0);

                    if (variablesAtAllSteps != null)
                    {
                        variablesAtAllSteps.TryAdd(calculationType, variablesAtAllStepsForMethod);
                    }
                });

                calculationTask.Start();
                calculationTasks.Add(calculationTask);
            }

            await Task.WhenAll(calculationTasks);

            return timeResults;
        }
    }
}
