// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Tensorflow.Serving;

namespace AML.Client
{
    public interface IScoringRequest
    {
        PredictRequest MakePredictRequest();
    }
}