using Content.Shared.ADT.Virology;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem
{
    private void EnsureSymptomStates(ADTDisease disease)
    {
        disease.SymptomStates ??= new List<ADTSymptomState>();
        if (disease.SymptomStates.Count == disease.Symptoms.Count)
            return;

        var states = new List<ADTSymptomState>();
        foreach (var symptom in disease.Symptoms)
        {
            var existing = disease.SymptomStates.Find(s => s.Id == symptom);
            states.Add(existing ?? new ADTSymptomState(symptom));
        }

        disease.SymptomStates = states;
    }

    private void ActivateSymptoms(EntityUid uid, ADTDisease disease)
    {
        if (disease.Symptoms.Count == 0)
            return;

        EnsureSymptomStates(disease);

        if (!disease.SymptomsStarted)
        {
            disease.SymptomsStarted = true;
            RunSymptoms(uid, disease, ADTSymptomPhase.Start);
        }

        RunSymptoms(uid, disease, ADTSymptomPhase.Activate);
    }

    private void EndSymptoms(EntityUid uid, ADTDisease disease)
    {
        if (!disease.SymptomsStarted)
            return;

        EnsureSymptomStates(disease);
        RunSymptoms(uid, disease, ADTSymptomPhase.End);
        disease.SymptomsStarted = false;
    }

    private void RunSymptoms(EntityUid uid, ADTDisease disease, ADTSymptomPhase phase)
    {
        foreach (var state in disease.SymptomStates!.ToArray())
        {
            if (TerminatingOrDeleted(uid))
                return;

            if (!_proto.TryIndex(state.Id, out var symptom))
                continue;

            switch (phase)
            {
                case ADTSymptomPhase.Start when symptom.OnStart != null:
                    _effects.ApplyEffects(uid, symptom.OnStart);
                    break;
                case ADTSymptomPhase.End when symptom.OnEnd != null:
                    _effects.ApplyEffects(uid, symptom.OnEnd);
                    break;
                case ADTSymptomPhase.Activate:
                {
                    foreach (var stage in symptom.Stages)
                    {
                        if (disease.Stage < stage.MinStage || disease.Stage > stage.MaxStage)
                            continue;

                        _effects.ApplyEffects(uid, stage.Effects);
                    }

                    break;
                }
            }

            foreach (var effect in symptom.DiseaseEffects)
            {
                if (effect.Phase != phase)
                    continue;

                if (phase == ADTSymptomPhase.Activate && (disease.Stage < effect.MinStage || disease.Stage > effect.MaxStage))
                    continue;

                if (effect.Probability < 1f && !_random.Prob(effect.Probability))
                    continue;

                effect.Effect.Raise(EntityManager, uid, disease, state, phase);
            }
        }
    }
}
