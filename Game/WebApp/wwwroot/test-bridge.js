// Test bridge for Blazor WebAssembly testing
// Provides JavaScript functions that call back into Blazor components

window.testBridge = {
    _componentRef: null,
    _initialized: false,

    initialize: function (componentRef) {
        this._componentRef = componentRef;
        this._initialized = true;
        console.log('Test bridge initialized');
    },

    step: async function () {
        if (!this._initialized || !this._componentRef) {
            throw new Error('Test bridge not initialized');
        }
        await this._componentRef.invokeMethodAsync('TestStep');
    },

    executeCommand: async function (commandType, targetId, parameters) {
        if (!this._initialized || !this._componentRef) {
            throw new Error('Test bridge not initialized');
        }

        switch (commandType) {
            case 'Spawn':
                // Handle player spawn
                if (parameters.type === undefined || parameters.type === 'Player') {
                    await this._componentRef.invokeMethodAsync('TestSpawn',
                        targetId || 'player-1',
                        parameters.name || 'Player',
                        parameters.x || 0,
                        parameters.y || 0,
                        parameters.health || 100);
                }
                break;
            case 'Move':
                await this._componentRef.invokeMethodAsync('TestMove', 
                    parameters.deltaX || 0, 
                    parameters.deltaY || 0);
                break;
            case 'Damage':
                await this._componentRef.invokeMethodAsync('TestTakeDamage', 
                    parameters.amount || 0);
                break;
            case 'Heal':
                await this._componentRef.invokeMethodAsync('TestHeal', 
                    parameters.amount || 0);
                break;
            case 'UpdateAI':
                await this._componentRef.invokeMethodAsync('TestStep');
                break;
            default:
                console.warn('Unknown command type:', commandType);
        }
    },

    getSnapshot: async function () {
        if (!this._initialized || !this._componentRef) {
            throw new Error('Test bridge not initialized');
        }
        return await this._componentRef.invokeMethodAsync('TestGetSnapshot');
    }
};
