﻿using DigitR.Core.InputProvider;
using DigitR.Core.NeuralNetwork.Behaviours;

namespace DigitR.Core.NeuralNetwork
{
    /// <summary>
    /// Provides an interface of neural network.
    /// </summary>
    public interface INeuralNetwork<in TInput, out TOuput> : ITrainable<TInput>, IPersistable
    {
        /// <summary>
        /// Provides a determination logic according to input pattern 
        /// and current state of this instance of neuran network.
        /// </summary>
        /// <param name="inputPattern">The input pattern for determine.</param>
        /// <returns>The result successful flag.</returns>
        TOuput Process(IInputPattern<TInput> inputPattern);
    }
}
