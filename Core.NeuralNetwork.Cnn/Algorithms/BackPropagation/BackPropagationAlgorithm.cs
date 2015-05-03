﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using DigitR.Common.Logging;
using DigitR.Core.InputProvider;
using DigitR.Core.NeuralNetwork.Algorithms;
using DigitR.Core.NeuralNetwork.Cnn.Algorithms.BackPropagation.Steps;
using DigitR.Core.NeuralNetwork.Cnn.Algorithms.BackPropagation.Steps.Implementation;

namespace DigitR.Core.NeuralNetwork.Cnn.Algorithms.BackPropagation
{
    public class BackPropagationAlgorithm
        : ITrainingAlgorithm<
            INeuralNetwork<double[]>,
            IInputTrainingPattern<double[]>>
    {
        private const double ErrorEps = 0.00001;
        private const double MinErrorEnergy = 10;

        private readonly IReadOnlyCollection<IPropagationStep> algorithmSteps;

        private double prevErrorEnergy;
        private double currentErrorEnergy;


        public BackPropagationAlgorithm(
            IActivationAlgorithm<double, double> activationAlgorithm)
        {
            prevErrorEnergy = 0;
            currentErrorEnergy = Double.MaxValue;

            algorithmSteps = new ReadOnlyCollection<IPropagationStep>(
                new List<IPropagationStep>
                {
                    new SetNetworkInputStep(),
                    new ForwardPropagateLayersStep(activationAlgorithm),
                    new BackPropagateOutputLayerStep(activationAlgorithm),
                    new BackPropagateHiddenLayersStep(activationAlgorithm),
                    new CommitWeightCorrectionsStep()
                });
        }

        public bool ProcessTraining(
            INeuralNetwork<double[]> network,
            IEnumerable<IInputTrainingPattern<double[]>> patterns)
        {
            IMultiLayerNeuralNetwork<double> multiLayerNeuralNetwork = (IMultiLayerNeuralNetwork<double>)network;

            int patternsCount = 0;
            double energySum = 0;

            foreach (IInputTrainingPattern<double[]> pattern in patterns)
            {
                Log.Current.Info("Pattern #{0} started to processing.", patternsCount + 1);

                ProcessPattern(multiLayerNeuralNetwork, pattern);

                double[] realOutputs = multiLayerNeuralNetwork.Layers
                    .First(layer => layer.IsLast).Neurons
                    .Select(neuron => neuron.Output)
                    .ToArray();

                Log.Current.Info("Desired output = {0}, real output = {1}",
                    pattern.Label.Aggregate(new StringBuilder(), (builder, element) => builder.AppendFormat(" {0}", element)).ToString(),
                    realOutputs.Aggregate(new StringBuilder(), (builder, element) => builder.AppendFormat(" {0}", element)).ToString());

                energySum += CalculateErrorEnergy(realOutputs, pattern.Label);

                Log.Current.Info("Energy sum = {0}", energySum);
                ++patternsCount;
            }

            currentErrorEnergy = energySum / patternsCount;

            bool trained = currentErrorEnergy <= MinErrorEnergy && Math.Abs(prevErrorEnergy - currentErrorEnergy) <= ErrorEps;

            Log.Current.Info("Current error energy = {0}, previous error energy = {1}", currentErrorEnergy, prevErrorEnergy);

            prevErrorEnergy = currentErrorEnergy;
            currentErrorEnergy = 0;

            return trained;
        }

        private void ProcessPattern(
            IMultiLayerNeuralNetwork<double> network,
            IInputTrainingPattern<double[]> pattern)
        {
            foreach (IPropagationStep propagationStep in algorithmSteps)
            {
                propagationStep.Process(network, pattern);
            }
        }

        private double CalculateErrorEnergy(double[] realOutput, double[] desiredOuput)
        {
            Contract.Assert(realOutput.Length == desiredOuput.Length, "The length of real and desired outputs does not match.");

            double errorsSum = 0;

            for (int outputIndex = 0; outputIndex < realOutput.Length; outputIndex++)
            {
                errorsSum += Math.Pow(desiredOuput[outputIndex] - realOutput[outputIndex], 2);
            }

            double energy = errorsSum / 2;

            return energy;
        }
    }
}