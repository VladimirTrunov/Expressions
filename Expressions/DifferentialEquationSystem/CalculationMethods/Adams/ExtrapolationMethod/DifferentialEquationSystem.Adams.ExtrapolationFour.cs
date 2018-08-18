﻿namespace DifferentialEquationSystem
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Expressions.Models;

    public partial class DifferentialEquationSystem
    {
        /// <summary>
        /// Method calculates a differential equation system with Extrapolation Adams Four method
        /// </summary>
        /// <param name="variablesAtAllStep">Container where the intermediate parameters are supposed to be saved</param>
        /// <returns>List of result variables</returns>
        private List<InitVariable> AdamsExtrapolationFourSync(List<List<InitVariable>> variablesAtAllStep = null)
        {
            #region Calculation preparation

            List<Variable> allVars;
            List<Variable> currentLeftVariables = new List<Variable>();
            List<Variable> nextLeftVariables = new List<Variable>();

            // Copy this.LeftVariables to the current one and to the nex one
            // To leave this.LeftVariables member unchanged (for further calculations)
            DifferentialEquationSystem.CopyVariables(this.LeftVariables, currentLeftVariables);
            DifferentialEquationSystem.CopyVariables(this.LeftVariables, nextLeftVariables);

            // Setting of current time (to leave this.TimeVariable unchanged)
            Variable currentTime = new Variable(this.TimeVariable);

            // If it is required to save intermediate calculations - save the start values
            if (variablesAtAllStep != null)
            {
                // This is the first record for intermediate calculations containier
                // It has to be clear
                variablesAtAllStep.Clear();
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, this.LeftVariables, currentTime);
            }

            #endregion

            #region First variables

            DifferentialEquationSystem differentialEquationSystem = new DifferentialEquationSystem(this.ExpressionSystem, this.LeftVariables, this.Constants,
                this.TimeVariable, this.TimeVariable.Value + 4 * this.Tau, this.Tau);
            List<List<InitVariable>> firstVariables = new List<List<InitVariable>>();

            differentialEquationSystem.Calculate(CalculationTypeName.Euler, out List<InitVariable> bufer, firstVariables);

            List<Variable> firstLeftVariables;
            List<Variable> secondLeftVariables;
            List<Variable> thirdLeftVariables;
            List<Variable> forthLeftVariables;

            firstLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[1]);
            secondLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[2]);
            thirdLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[3]);
            forthLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[4]);

            firstLeftVariables.RemoveAt(firstLeftVariables.Count - 1);
            secondLeftVariables.RemoveAt(secondLeftVariables.Count - 1);
            thirdLeftVariables.RemoveAt(thirdLeftVariables.Count - 1);
            forthLeftVariables.RemoveAt(forthLeftVariables.Count - 1);

            if (variablesAtAllStep != null)
            {
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, firstLeftVariables, new Variable(currentTime.Name, currentTime.Value + this.Tau));
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, secondLeftVariables, new Variable(currentTime.Name, currentTime.Value + 2 * this.Tau));
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, thirdLeftVariables, new Variable(currentTime.Name, currentTime.Value + 3 * this.Tau));
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, forthLeftVariables, new Variable(currentTime.Name, currentTime.Value + 4 * this.Tau));
            }

            #endregion

            double[,] Q = new double[5, this.ExpressionSystem.Count];

            allVars = DifferentialEquationSystem.CollectVariables(currentLeftVariables, this.Constants, currentTime);
            for (int i = 0; i < this.ExpressionSystem.Count; i++)
            {
                Q[0, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            }

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(firstLeftVariables, this.Constants, currentTime);

            for (int i = 0; i < this.ExpressionSystem.Count; i++)
            {
                Q[1, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            }

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(secondLeftVariables, this.Constants, currentTime);

            for (int i = 0; i < this.ExpressionSystem.Count; i++)
            {
                Q[2, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            }

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(thirdLeftVariables, this.Constants, currentTime);
            for (int i = 0; i < this.ExpressionSystem.Count; i++)
            {
                Q[3, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            }

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(forthLeftVariables, this.Constants, currentTime);
            for (int i = 0; i < this.ExpressionSystem.Count; i++)
            {
                Q[4, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            }

            DifferentialEquationSystem.CopyVariables(forthLeftVariables, currentLeftVariables);

            do
            {
                for (int i = 0; i < nextLeftVariables.Count; i++)
                {
                    nextLeftVariables[i].Value = currentLeftVariables[i].Value + 1.0 / 720 * (1901 * Q[4, i] - 2774 * Q[3, i] + 2616 * Q[2, i] - 1274 * Q[1, i] + 251 * Q[0, i]);
                }

                allVars = DifferentialEquationSystem.CollectVariables(nextLeftVariables, this.Constants, new Variable(currentTime.Name, currentTime.Value + this.Tau));

                for (int i = 0; i < nextLeftVariables.Count; i++)
                {
                    Q[0, i] = Q[1, i];
                    Q[1, i] = Q[2, i];
                    Q[2, i] = Q[3, i];
                    Q[3, i] = Q[4, i];
                    Q[4, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
                }

                if (variablesAtAllStep != null)
                {
                    DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, nextLeftVariables,
                        new Variable(currentTime.Name, currentTime.Value + this.Tau));
                }

                DifferentialEquationSystem.CopyVariables(nextLeftVariables, currentLeftVariables);

                currentTime.Value += this.Tau;
            } while (currentTime.Value < this.TEnd);

            List<InitVariable> result = new List<InitVariable>();
            DifferentialEquationSystem.CopyVariables(currentLeftVariables, result);
            return result;
        }

        /// <summary>
        /// Method calculates a differential equation system with Extrapolation Adams Four method
        /// </summary>
        /// <param name="variablesAtAllStep">Container where the intermediate parameters are supposed to be saved</param>
        /// <returns>List of result variables</returns>
        private List<InitVariable> AdamsExtrapolationFourAsync(List<List<InitVariable>> variablesAtAllStep = null)
        {
            #region Calculation preparation

            List<Variable> allVars;
            List<Variable> currentLeftVariables = new List<Variable>();
            List<Variable> nextLeftVariables = new List<Variable>();

            // Copy this.LeftVariables to the current one and to the nex one
            // To leave this.LeftVariables member unchanged (for further calculations)
            DifferentialEquationSystem.CopyVariables(this.LeftVariables, currentLeftVariables);
            DifferentialEquationSystem.CopyVariables(this.LeftVariables, nextLeftVariables);

            // Setting of current time (to leave this.TimeVariable unchanged)
            Variable currentTime = new Variable(this.TimeVariable);

            // If it is required to save intermediate calculations - save the start values
            if (variablesAtAllStep != null)
            {
                // This is the first record for intermediate calculations containier
                // It has to be clear
                variablesAtAllStep.Clear();
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, this.LeftVariables, currentTime);
            }

            #endregion

            #region First variables

            DifferentialEquationSystem differentialEquationSystem = new DifferentialEquationSystem(this.ExpressionSystem, this.LeftVariables, this.Constants,
                this.TimeVariable, this.TimeVariable.Value + 4 * this.Tau, this.Tau);
            List<List<InitVariable>> firstVariables = new List<List<InitVariable>>();

            differentialEquationSystem.Calculate(CalculationTypeName.Euler, out List<InitVariable> bufer, firstVariables);

            List<Variable> firstLeftVariables;
            List<Variable> secondLeftVariables;
            List<Variable> thirdLeftVariables;
            List<Variable> forthLeftVariables;

            firstLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[1]);
            secondLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[2]);
            thirdLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[3]);
            forthLeftVariables = DifferentialEquationSystem.ConvertInitVariablesToVariables(firstVariables[4]);

            firstLeftVariables.RemoveAt(firstLeftVariables.Count - 1);
            secondLeftVariables.RemoveAt(secondLeftVariables.Count - 1);
            thirdLeftVariables.RemoveAt(thirdLeftVariables.Count - 1);
            forthLeftVariables.RemoveAt(forthLeftVariables.Count - 1);

            if (variablesAtAllStep != null)
            {
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, firstLeftVariables, new Variable(currentTime.Name, currentTime.Value + this.Tau));
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, secondLeftVariables, new Variable(currentTime.Name, currentTime.Value + 2 * this.Tau));
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, thirdLeftVariables, new Variable(currentTime.Name, currentTime.Value + 3 * this.Tau));
                DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, forthLeftVariables, new Variable(currentTime.Name, currentTime.Value + 4 * this.Tau));
            }

            #endregion

            double[,] Q = new double[5, this.ExpressionSystem.Count];

            allVars = DifferentialEquationSystem.CollectVariables(currentLeftVariables, this.Constants, currentTime);
            Parallel.For(0, this.ExpressionSystem.Count, (i) => 
            {
                Q[0, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            });

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(firstLeftVariables, this.Constants, currentTime);

            Parallel.For(0, this.ExpressionSystem.Count, (i) => 
            {
                Q[1, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            });            

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(secondLeftVariables, this.Constants, currentTime);

            Parallel.For(0, this.ExpressionSystem.Count, (i) => 
            {
                Q[2, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            });

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(thirdLeftVariables, this.Constants, currentTime);

            Parallel.For(0, this.ExpressionSystem.Count, (i) => 
            {
                Q[3, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            });

            currentTime.Value += this.Tau;
            allVars = DifferentialEquationSystem.CollectVariables(forthLeftVariables, this.Constants, currentTime);

            Parallel.For(0, this.ExpressionSystem.Count, (i) => 
            {
                Q[4, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
            });

            DifferentialEquationSystem.CopyVariables(forthLeftVariables, currentLeftVariables);

            do
            {
                Parallel.For(0, nextLeftVariables.Count, (i) => 
                {
                    nextLeftVariables[i].Value = currentLeftVariables[i].Value + 1.0 / 720 * (1901 * Q[4, i] - 2774 * Q[3, i] + 2616 * Q[2, i] - 1274 * Q[1, i] + 251 * Q[0, i]);
                });

                allVars = DifferentialEquationSystem.CollectVariables(nextLeftVariables, this.Constants, new Variable(currentTime.Name, currentTime.Value + this.Tau));

                Parallel.For(0, nextLeftVariables.Count, (i) => 
                {
                    Q[0, i] = Q[1, i];
                    Q[1, i] = Q[2, i];
                    Q[2, i] = Q[3, i];
                    Q[3, i] = Q[4, i];
                    Q[4, i] = this.Tau * this.ExpressionSystem[i].GetResultValue(allVars);
                });                

                if (variablesAtAllStep != null)
                {
                    DifferentialEquationSystem.SaveLeftVariableToStatistics(variablesAtAllStep, nextLeftVariables,
                        new Variable(currentTime.Name, currentTime.Value + this.Tau));
                }

                DifferentialEquationSystem.CopyVariables(nextLeftVariables, currentLeftVariables);

                currentTime.Value += this.Tau;
            } while (currentTime.Value < this.TEnd);

            List<InitVariable> result = new List<InitVariable>();
            DifferentialEquationSystem.CopyVariables(currentLeftVariables, result);
            return result;
        }
    }
}
