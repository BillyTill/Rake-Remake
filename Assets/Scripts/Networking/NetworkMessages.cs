using System;
using UnityEngine;

namespace Lake.Networking
{
    /// <summary>
    /// Ввод клиента: только намерение, без позиции и без результатов.
    /// Тик — номер команды, а не абсолютное время: часы клиента и хоста
    /// не обязаны совпадать.
    /// </summary>
    [Serializable]
    public struct PlayerInputCommand
    {
        public uint sequence;
        public Vector2 move;
        public float yaw;
        public float pitch;
        public byte buttons;

        /// <summary>WeaponId как байт: какой ствол в руках в момент команды.</summary>
        public byte weapon;

        /// <summary>
        /// Полный прицел: куда целится стрелок (тело + невидимый прицел + отдача).
        /// Тело на сервере и чужих клонах крутится по yaw, а оружие — по
        /// расхождению yaw→aimYaw / pitch→aimPitch: новый «плечевой» доворот
        /// оружия виден всем, и в мёртвой зоне камера чужого не дёргается.
        /// </summary>
        public float aimYaw;
        public float aimPitch;
    }

    [Flags]
    public enum InputButtons : byte
    {
        None = 0,
        Sprint = 1 << 0,
        Crouch = 1 << 1,
        Jump = 1 << 2,
        Interact = 1 << 3,
        Flashlight = 1 << 4,
        Ads = 1 << 5,
    }

    [Flags]
    public enum PlayerStateFlags : byte
    {
        None = 0,
        Crouching = 1 << 0,
        Sprinting = 1 << 1,
        Grounded = 1 << 2,
        Ads = 1 << 3,
    }

    /// <summary>
    /// Состояние одного игрока в снапшоте. lastProcessedCommand имеет смысл
    /// только для получателя: по нему клиент сверяет предсказание.
    /// Флаги описывают позу чужого игрока (присед/спринт/опора/прицел),
    /// а стамина — авторитетный запас выносливости: по ней reconcile
    /// восстанавливает мотор. weapon и pitch нужны чужому визуалу:
    /// какой ствол в руках и как он доворачивается по вертикали.
    /// </summary>
    [Serializable]
    public struct PlayerSnapshot
    {
        public uint netId;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public uint lastProcessedCommand;
        public byte flags;

        /// <summary>Выносливость, нормированная в 0…255 серверной стороной.</summary>
        public byte stamina;

        /// <summary>WeaponId:.byte — какой ствол в руках (0 — ничего).</summary>
        public byte weapon;

        /// <summary>
        /// Полный прицел чужого игрока: расхождение yaw→aimYaw / pitch→aimPitch
        /// разворачивает оружие на чужом экране тем же «плечевым» правилом,
        /// что и у владельца (pitch остаётся наклоном пивота).
        /// </summary>
        public float aimYaw;
        public float aimPitch;
    }

    [Serializable]
    public struct SnapshotMessage
    {
        public PlayerSnapshot[] players;
    }
}
