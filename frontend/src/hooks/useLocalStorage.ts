import { useState } from 'react';

/**
 * Custom hook for managing localStorage with React state
 * @param key - The localStorage key
 * @param initialValue - The initial value if key doesn't exist
 * @returns [value, setValue] tuple similar to useState
 */
export function useLocalStorage<T>(key: string, initialValue: T): [T, (value: T) => void] {
  // Get initial value from localStorage or use provided initialValue
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const item = window.localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch (error) {
      console.error(`Error reading localStorage key "${key}":`, error);
      return initialValue;
    }
  });

  // Update localStorage when state changes
  const setValue = (value: T) => {
    try {
      setStoredValue(value);
      window.localStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      console.error(`Error setting localStorage key "${key}":`, error);
    }
  };

  return [storedValue, setValue];
}

/**
 * Custom hook for boolean localStorage values
 */
export function useBooleanLocalStorage(key: string, initialValue: boolean = false) {
  const [storedValue, setStoredValue] = useState<boolean>(() => {
    try {
      const item = window.localStorage.getItem(key);
      return item === 'true';
    } catch (error) {
      console.error(`Error reading localStorage key "${key}":`, error);
      return initialValue;
    }
  });

  const setValue = (value: boolean) => {
    try {
      setStoredValue(value);
      window.localStorage.setItem(key, value.toString());
    } catch (error) {
      console.error(`Error setting localStorage key "${key}":`, error);
    }
  };

  return [storedValue, setValue] as const;
}
