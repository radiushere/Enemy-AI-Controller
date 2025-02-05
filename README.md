<h2>Overview</h2>
    <p>
      The <strong>BossController</strong> script is a Unity C# script that manages a boss enemy's behavior.
      It determines whether the boss should chase the player, flee from the player, or throw a projectile when the player is very close.
    </p>
    
  <h2>Key Features</h2>
    <ul>
      <li><strong>State Management:</strong> The script defines three states:
        <ul>
          <li><em>Chasing</em> – The boss moves towards the player when the distance exceeds a specified range.</li>
          <li><em>Fleeing</em> – The boss flees when the player is within a certain range but not too close. An extra speed bonus may be applied if the player is very near.</li>
          <li><em>Throwing</em> – The boss stops moving, faces the player, and throws a projectile when the player is too close.</li>
        </ul>
      </li>
      <li>
        <strong>Dynamic Projectile Speed:</strong> The speed of the thrown projectile is dynamically adjusted based on the distance to the player.
      </li>
      <li>
        <strong>Animation &amp; Navigation:</strong> Integrates with Unity's Animator (using parameters like "isRunning" and "isThrowing") and NavMeshAgent for smooth navigation and animation transitions.
      </li>
      <li>
        <strong>Win Condition:</strong> On colliding with the player, the script interacts with a win trigger component (<code>SnailWinTrigger</code>) to update the enemy count.
      </li>
    </ul>
    
  <h2>Usage</h2>
    <ol>
      <li>
        Attach the script to your boss enemy GameObject in Unity.
      </li>
      <li>
        Ensure the boss has both a <code>NavMeshAgent</code> and an <code>Animator</code> component. The Animator should include the parameters "isRunning" and "isThrowing".
      </li>
      <li>
        Assign the player's Transform to the <code>player</code> field in the Inspector.
      </li>
      <li>
        Configure the settings in the Inspector (such as <code>runRange</code>, <code>minDistance</code>, <code>throwCooldown</code>, <code>minProjectileSpeed</code>, and <code>maxProjectileSpeed</code>).
      </li>
      <li>
        Assign a projectile prefab and set a <code>throwOrigin</code> Transform from which the projectile will be instantiated.
      </li>
      <li>
        Make sure your scene has a properly baked NavMesh for the NavMeshAgent to function correctly.
      </li>
    </ol>
    
  <h2>Additional Notes</h2>
    <p>
      The script uses a hysteresis value to prevent rapid state switching and ensures smooth transitions between states.
      It also handles win condition updates by interacting with a separate <code>SnailWinTrigger</code> component when the boss collides with the player.
    </p>
