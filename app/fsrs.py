"""
FSRS-6 Algorithm Implementation
Based on the specification provided
"""
import math
from datetime import datetime, timedelta

class FSRS:
    """Free Spaced Repetition Scheduler (FSRS-6)"""
    
    # Default FSRS parameters (w0-w19, w20)
    DEFAULT_PARAMS = [
        0.4072,  # w0: initial stability for Again
        1.1829,  # w1: initial stability for Hard
        3.1262,  # w2: initial stability for Good
        15.4722, # w3: initial stability for Easy
        7.2102,  # w4: default difficulty (Good)
        0.5316,  # w5: difficulty weight for Again
        1.0651,  # w6: difficulty weight for Hard
        0.0234,  # w7: difficulty weight for Easy
        1.616,   # w8: stability factor scale
        0.1544,  # w9: stability decay rate
        0.9221,  # w10: retrievability factor in lapse formula
        2.0063,  # w11: lapse stability scale
        0.2272,  # w12: lapse stability difficulty factor
        0.2281,  # w13: lapse stability retrievability factor
        1.5662,  # w14: SInc scale factor
        0.0,     # w15: Hard penalty (0 < w15 < 1)
        2.9469,  # w16: Easy bonus (1 < w16 < 6)
        0.2272,  # w17: short-term stability grade weight
        2.8284,  # w18: short-term stability grade scale
        0.0,     # w19: short-term stability decay
        0.15     # w20: forgetting curve personalization (0.1-0.8)
    ]
    
    def __init__(self, params=None, desired_retention=0.9):
        """
        Initialize FSRS scheduler
        
        Args:
            params: List of 21 FSRS parameters (w0-w20), uses defaults if None
            desired_retention: Target retention rate (default 0.9 = 90%)
        """
        self.w = params if params is not None else self.DEFAULT_PARAMS
        self.desired_retention = desired_retention
        self.DECAY = -0.5  # Used in power function for forgetting curve
        self.FACTOR = 0.9 ** (1 / self.DECAY) - 1  # Constant factor for retrievability
    
    def calculate_retrievability(self, elapsed_days, stability):
        """
        Calculate retrievability (R) using the power-based forgetting curve
        
        R = (1 + FACTOR * (t/S) ^ DECAY) ^ DECAY
        
        where w20 affects the curve shape
        """
        if stability <= 0:
            return 1.0
        
        if elapsed_days <= 0:
            return 1.0
        
        # Power function forgetting curve with w20 personalization
        # R = (1 + FACTOR * (t/S)^DECAY)^DECAY
        t_over_s = elapsed_days / stability
        base = 1 + self.FACTOR * math.pow(t_over_s, self.DECAY)
        r = math.pow(base, self.DECAY)
        
        return max(0.0, min(1.0, r))
    
    def calculate_interval(self, stability, desired_retention=None):
        """
        Calculate the interval based on stability and desired retention
        
        I = S / FACTOR * ((DR ^ (1/DECAY)) - 1)
        
        When DR = 0.9, I ≈ S
        """
        if desired_retention is None:
            desired_retention = self.desired_retention
        
        # I = S / FACTOR * ((DR^(1/DECAY)) - 1)
        interval = stability / self.FACTOR * (math.pow(desired_retention, 1 / self.DECAY) - 1)
        
        return max(0.1, interval)  # Minimum 0.1 day
    
    def init_difficulty(self, grade):
        """
        Calculate initial difficulty after first review
        
        D0 = w4 - w5 * (G - 3)
        where G is grade: Again=1, Hard=2, Good=3, Easy=4
        
        Output is clamped to [1, 10]
        """
        d0 = self.w[4] - self.w[5] * (grade - 3)
        return max(1.0, min(10.0, d0))
    
    def init_stability(self, grade):
        """
        Get initial stability based on first grade
        
        w0: Again, w1: Hard, w2: Good, w3: Easy
        """
        if grade == 1:  # Again
            return self.w[0]
        elif grade == 2:  # Hard
            return self.w[1]
        elif grade == 3:  # Good
            return self.w[2]
        else:  # Easy (4)
            return self.w[3]
    
    def next_difficulty(self, d, grade):
        """
        Calculate next difficulty with linear damping and mean reversion
        
        Steps:
        1. Calculate change based on grade
        2. Apply linear damping (as D approaches 10, updates get smaller)
        3. Apply mean reversion (revert slightly toward w4)
        """
        # Step 1: Calculate change based on grade
        if grade == 1:  # Again
            delta_d = -self.w[6] * (grade - 3)
        elif grade == 2:  # Hard
            delta_d = -self.w[6] * (grade - 3)
        elif grade == 4:  # Easy
            delta_d = -self.w[7] * (grade - 3)
        else:  # Good (3)
            delta_d = 0
        
        # Step 2: Linear damping
        # As D approaches 10, each update gets smaller
        # D' = D + delta_d * (10 - D) / 9
        d_prime = d + delta_d * (10 - d) / 9
        
        # Step 3: Mean reversion toward w4
        # D'' = w5 * D' + (1 - w5) * w4
        mean_reversion = 0.5  # Factor for mean reversion
        d_double_prime = mean_reversion * d_prime + (1 - mean_reversion) * self.w[4]
        
        return max(1.0, min(10.0, d_double_prime))
    
    def next_stability(self, s, d, r, grade, is_lapse=False):
        """
        Calculate next stability
        
        For successful reviews (Hard, Good, Easy):
        S' = S * SInc
        where SInc = 1 + e^w8 * (11 - D) * S^(-w9) * (e^(w10 * (1-R)) - 1) * w15/w16
        
        For lapses (Again):
        S' = min(S, w11 * D^(-w12) * ((S+1)^w13 - 1) * e^(w10 * (1-R)))
        """
        if is_lapse or grade == 1:  # Again
            # Lapse formula
            # S' = min(S, w11 * D^(-w12) * ((S+1)^w13 - 1) * e^(w10 * (1-R)))
            new_s = self.w[11] * math.pow(d, -self.w[12]) * (math.pow(s + 1, self.w[13]) - 1) * math.exp(self.w[10] * (1 - r))
            return min(s, max(0.1, new_s))
        else:
            # Success formula
            # Calculate SInc components
            
            # f(D) = 11 - D (linear)
            f_d = 11 - d
            
            # f(S) = S^(-w9) (power decay)
            f_s = math.pow(s, -self.w[9]) if s > 0 else 1.0
            
            # f(R) = e^(w10 * (1-R)) - 1
            f_r = math.exp(self.w[10] * (1 - r)) - 1
            
            # Grade factors
            if grade == 2:  # Hard
                w15_factor = 0.5  # Hard penalty
                w16_factor = 1.0
            elif grade == 4:  # Easy
                w15_factor = 1.0
                w16_factor = self.w[16]  # Easy bonus
            else:  # Good (3)
                w15_factor = 1.0
                w16_factor = 1.0
            
            # SInc = 1 + e^w8 * f(D) * f(S) * f(R) * w15 * w16
            s_inc = 1 + math.exp(self.w[8]) * f_d * f_s * f_r * w15_factor * w16_factor
            
            # S' = S * SInc
            new_s = s * s_inc
            
            return max(0.1, new_s)
    
    def schedule_card(self, grade, card_state):
        """
        Schedule a card based on grade and current state
        
        Args:
            grade: 1=Again, 2=Hard, 3=Good, 4=Easy
            card_state: dict with keys: state, stability, difficulty, retrievability, 
                       last_review, reps, lapses
        
        Returns:
            dict with updated state, stability, difficulty, interval, due_date, etc.
        """
        state = card_state.get('state', 'new')
        s = card_state.get('stability', 0.0)
        d = card_state.get('difficulty', 5.0)
        r = card_state.get('retrievability', 1.0)
        last_review = card_state.get('last_review')
        reps = card_state.get('reps', 0)
        lapses = card_state.get('lapses', 0)
        
        # For first review
        if state == 'new' or reps == 0:
            new_s = self.init_stability(grade)
            new_d = self.init_difficulty(grade)
            new_state = 'learning' if grade == 1 else 'review'
            new_lapses = lapses + 1 if grade == 1 else lapses
        else:
            # Calculate elapsed time and retrievability
            if last_review:
                elapsed = (datetime.utcnow() - last_review).total_seconds() / 86400.0  # days
                r = self.calculate_retrievability(elapsed, s)
            else:
                elapsed = 0
                r = 1.0
            
            # Update difficulty
            new_d = self.next_difficulty(d, grade)
            
            # Update stability
            is_lapse = (grade == 1)
            new_s = self.next_stability(s, d, r, grade, is_lapse)
            
            # Update state
            if grade == 1:  # Again
                new_state = 'relearning'
                new_lapses = lapses + 1
            else:
                new_state = 'review'
                new_lapses = lapses
        
        # Calculate new interval and due date
        new_interval = self.calculate_interval(new_s)
        new_due = datetime.utcnow() + timedelta(days=new_interval)
        
        return {
            'state': new_state,
            'stability': new_s,
            'difficulty': new_d,
            'retrievability': r,
            'interval': new_interval,
            'due_date': new_due,
            'reps': reps + 1,
            'lapses': new_lapses,
            'last_review': datetime.utcnow()
        }