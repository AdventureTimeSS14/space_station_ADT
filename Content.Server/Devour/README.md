# Enhanced Devour System

This system provides a comprehensive devouring mechanic for Space Station 14, allowing characters to choose their role in the food chain and experience detailed digestion mechanics.

## Features

### Character Roles
- **Predator**: Can devour other characters and digest them for nutrients
- **Prey**: Can be devoured by predators with configurable resistance
- **Prey/Predator**: Hybrid role that can both devour and be devoured

### Core Systems

#### DevourDigestionSystem
- Handles the digestion of devoured prey
- Converts prey into nutrients and waste
- Processes digestion at regular intervals
- Integrates with the bloodstream system

#### DevourScatSystem
- Manages waste disposal through toilets
- Creates scat entities when waste is disposed
- Configurable scat production per predator
- Automatic toilet finding for forced disposal

#### EnhancedDevourSystem
- Integrates with the new prey/pred system
- Validates devour targets based on prey status
- Manages guts capacity and fullness
- Provides detailed feedback messages

### Components

#### PredatorComponent
- Defines predator capabilities
- Configurable digestion speed multipliers
- Scat production settings
- Living/dead prey digestion preferences

#### PreyComponent
- Defines prey characteristics
- Digestion resistance values
- State-based digestibility (alive/dead)
- Nutrient production settings

#### GutsComponent
- Container for digested materials
- Nutrient solution management
- Waste production tracking
- Capacity management

#### DevourSettingsComponent
- User preferences for the system
- Scat enable/disable options
- Message visibility controls
- Detailed digestion information toggles

## Usage

### For Players

1. **Character Creation**: Choose your role (Predator, Prey, or Prey/Predator) in the character setup
2. **Devouring**: Predators can devour valid prey using the devour action
3. **Digestion**: Devoured prey are automatically digested over time
4. **Waste Management**: Use toilets to dispose of digested waste
5. **Settings**: Configure your preferences for the devour system

### For Developers

#### Adding New Prey Types
```csharp
// Add PreyComponent to entity
var prey = AddComponent<PreyComponent>(entity);
prey.DigestibleWhileAlive = true;
prey.DigestibleWhileDead = true;
prey.DigestionResistance = 1.5f;
```

#### Customizing Predator Behavior
```csharp
// Add PredatorComponent to entity
var pred = AddComponent<PredatorComponent>(entity);
pred.CanDigestLiving = true;
pred.DigestionSpeedMultiplier = 1.2f;
pred.ProducesScat = true;
```

#### Integrating with Existing Systems
```csharp
// Check if entity is predator
if (HasComp<PredatorComponent>(entity))
{
    // Handle predator logic
}

// Check if entity is prey
if (HasComp<PreyComponent>(entity))
{
    // Handle prey logic
}
```

## Configuration

### Trait Selection
Players can choose their role through the character creation system:
- **Predator**: Full predator capabilities with guts system
- **Prey**: Can be devoured with configurable resistance
- **Prey/Predator**: Hybrid capabilities for versatile gameplay

### Scat System
The scat system is fully configurable:
- Enable/disable per player
- Automatic disposal when guts are full
- Configurable scat entity creation
- Toilet integration for waste disposal

### Message System
Players can customize which messages they see:
- Digestion progress updates
- Devour success/failure notifications
- Scat disposal confirmations
- Detailed digestion information

## Technical Details

### Performance
- Digestion processing occurs every 5 seconds
- Guts containers are optimized for minimal memory usage
- Scat entities auto-despawn after 5 minutes

### Networking
- All components are networked for multiplayer compatibility
- Settings are synchronized between client and server
- Real-time digestion updates are broadcast to relevant clients

### Integration
- Compatible with existing devour system
- Integrates with body systems (bloodstream, stomach)
- Works with existing toilet infrastructure
- Extensible for future enhancements

## Future Enhancements

- Advanced digestion mechanics (different prey types)
- Nutrient-based character growth
- Social dynamics between predators and prey
- Enhanced scat system with different waste types
- Integration with hunger/thirst systems
- Predator-prey relationship tracking

## Troubleshooting

### Common Issues

1. **Guts not working**: Ensure both PredatorComponent and GutsComponent are present
2. **Can't devour prey**: Check that target has PreyComponent and is in valid state
3. **Scat not appearing**: Verify scat is enabled in settings and toilet interaction works
4. **Digestion too slow/fast**: Adjust DigestionSpeedMultiplier in PredatorComponent

### Debug Commands
```csharp
// Force digestion cycle
devour_digest [entity]

// Clear guts
devour_clear_guts [entity]

// Set digestion speed
devour_set_speed [entity] [multiplier]
```

## Contributing

When contributing to this system:
1. Follow existing code patterns
2. Add appropriate tests for new functionality
3. Update documentation for any changes
4. Ensure multiplayer compatibility
5. Consider performance implications

## License

This system is part of Space Station 14 and follows the same licensing terms.
